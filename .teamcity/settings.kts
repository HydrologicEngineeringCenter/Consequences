import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetBuild
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetPack
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetTest
import jetbrains.buildServer.configs.kotlin.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.triggers.vcs
import jetbrains.buildServer.configs.kotlin.vcs.GitVcsRoot

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.

VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.

To debug settings scripts in command-line, run the

    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate

command and attach your debugger to the port 8000.

To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2026.1"

project {

    vcsRoot(Repo)

    buildType(SetVersion)
    buildType(SignExecutables)

    params {
        param("VersionBase", "0.1.0")
    }

    subProject(Deploy)
    subProject(Build)
    subProject(Endpoints)
}

object SetVersion : BuildType({
    name = "Set Version"
    description = "Computes Version from branch context: a v* tag yields the tag without its leading v, anything else yields %VersionBase%.<counter>-dev. No triggers - pulled into chains via snapshot dependency."

    buildNumberPattern = "%Version%"

    params {
        param("Version", "")
    }

    vcs {
        root(Repo)

        branchFilter = """
            +:<default>
            +:refs/tags/v*
            +:*
        """.trimIndent()
    }

    steps {
        powerShell {
            name = "Compute Version"
            scriptMode = script {
                content = """
                    ${'$'}branch = "%teamcity.build.branch%"
                    
                    if (${'$'}branch -match '^v') {
                        # Release tag: strip the leading 'v' (v1.2.3 -> 1.2.3)
                        ${'$'}version = ${'$'}branch -replace '^v', ''
                    } else {
                        # Snapshot: 0.1.0.<counter>-dev, mirroring the -dev scheme the GitHub Actions use.
                        ${'$'}version = "%VersionBase%.%build.counter%-dev"
                    }
                    
                    Write-Host "Branch:  ${'$'}branch"
                    Write-Host "Version: ${'$'}version"
                    
                    Write-Host "##teamcity[setParameter name='Version' value='${'$'}version']"
                    Write-Host "##teamcity[buildNumber '${'$'}version']"
                """.trimIndent()
            }
        }
    }

    requirements {
        contains("teamcity.agent.name", "windows")
        exists("DotNetCoreSDK9.0_Path")
    }
})

object SignExecutables : BuildType({
    templates(AbsoluteId("SignExecutables"))
    name = "Sign Binaries"
    description = """Authenticode-signs the two packable assemblies via the shared Root-level "Sign Binaries" template, ahead of Pack. Runs on a Linux agent (the signer is a container)."""

    artifactRules = "%TO_BE_SIGNED_DIR% => signed"
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${Build_Compile.depParamRefs["Version"]}")
        param("sign.filePatterns", "'Consequences.dll' 'Consequences.Network.dll'")
    }

    vcs {
        root(Repo)
    }

    dependencies {
        dependency(Build_Compile) {
            snapshot {
                reuseBuilds = ReuseBuilds.NO
                onDependencyFailure = FailureAction.FAIL_TO_START
            }

            artifacts {
                id = "ARTIFACT_DEPENDENCY_27"
                cleanDestination = true
                artifactRules = "TO_BE_SIGNED/** => %TO_BE_SIGNED_DIR%"
            }
        }
    }
})

object Repo : GitVcsRoot({
    name = "ConsequencesCore"
    url = "https://github.com/HydrologicEngineeringCenter/Consequences.git"
    branch = "main"
    branchSpec = """
        +:refs/heads/(main)
        +:refs/pull/(*/merge)
        +:refs/tags/(v*)
    """.trimIndent()
    useTagsAsBranches = true
    userForTags = "TeamCity <noreply@hecdev.net>"
})


object Build : Project({
    name = "Build"

    buildType(Build_Test)
    buildType(Build_Pack)
    buildType(Build_Compile)
})

object Build_Compile : BuildType({
    name = "Compile"
    description = "Builds the solution at the chain's version and publishes the two packable assemblies for signing."

    artifactRules = """
        Consequences/bin/Release/net9.0/Consequences.dll => TO_BE_SIGNED
        Consequences.Network/bin/Release/net9.0/Consequences.Network.dll => TO_BE_SIGNED
    """.trimIndent()
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${SetVersion.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    steps {
        dotnetBuild {
            name = "Build Solution"
            projects = "ConsequencesCore.slnx"
            configuration = "Release"
            args = "-p:Version=%Version% -p:ContinuousIntegrationBuild=true -p:RepositoryCommit=%build.vcs.number%"
        }
    }

    dependencies {
        snapshot(SetVersion) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }

    requirements {
        contains("teamcity.agent.name", "windows")
        exists("DotNetCoreSDK9.0_Path")
    }
})

object Build_Pack : BuildType({
    name = "Pack"
    description = "Rebuilds, swaps in the signed assemblies, then packs USACE.Consequences and USACE.Consequences.Network. Rebuilding is deliberate: pack --no-build needs obj/+bin/ in its own checkout, and signing happens on a different agent."

    artifactRules = "packages/*.nupkg => NuGets"
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${SignExecutables.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    steps {
        powerShell {
            name = "Clean package output"
            scriptMode = script {
                content = """
                    ${'$'}ErrorActionPreference = 'Stop'
                    
                    # TeamCity does not clean the checkout between builds, and PackageOutputPath is a
                    # fixed repo-root folder, so without this every Pack build would republish every
                    # .nupkg ever produced in this working directory.
                    if (Test-Path "packages") {
                        Write-Host "Removing stale package output"
                        Remove-Item -Path "packages" -Recurse -Force
                    }
                """.trimIndent()
            }
        }
        dotnetBuild {
            name = "Build Solution"
            projects = "ConsequencesCore.slnx"
            configuration = "Release"
            args = "-p:Version=%Version% -p:ContinuousIntegrationBuild=true -p:RepositoryCommit=%build.vcs.number%"
        }
        powerShell {
            name = "Swap in signed assemblies"
            scriptMode = script {
                content = """
                    ${'$'}ErrorActionPreference = 'Stop'
                    
                    # Overwrite the just-built assemblies with their signed counterparts so the
                    # packages produced by the next step carry signed DLLs. dotnet pack --no-build
                    # then packs exactly what is sitting in bin/.
                    ${'$'}map = @{
                        "signed\Consequences.dll"         = "Consequences\bin\Release\net9.0\Consequences.dll"
                        "signed\Consequences.Network.dll" = "Consequences.Network\bin\Release\net9.0\Consequences.Network.dll"
                    }
                    
                    foreach (${'$'}src in ${'$'}map.Keys) {
                        ${'$'}dst = ${'$'}map[${'$'}src]
                        if (-not (Test-Path ${'$'}src)) { throw "Signed assembly missing: ${'$'}src" }
                        if (-not (Test-Path ${'$'}dst)) { throw "Build output missing: ${'$'}dst" }
                        Write-Host "Replacing ${'$'}dst with signed ${'$'}src"
                        Copy-Item -Path ${'$'}src -Destination ${'$'}dst -Force
                    }
                """.trimIndent()
            }
        }
        dotnetPack {
            name = "Pack"
            projects = "ConsequencesCore.slnx"
            configuration = "Release"
            args = "--no-build -p:Version=%Version% -p:ContinuousIntegrationBuild=true -p:RepositoryCommit=%build.vcs.number%"
        }
    }

    dependencies {
        snapshot(Build_Test) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
        dependency(SignExecutables) {
            snapshot {
                reuseBuilds = ReuseBuilds.NO
                onDependencyFailure = FailureAction.FAIL_TO_START
            }

            artifacts {
                cleanDestination = true
                artifactRules = "signed/** => signed"
            }
        }
    }

    requirements {
        contains("teamcity.agent.name", "windows")
        exists("DotNetCoreSDK9.0_Path")
    }
})

object Build_Test : BuildType({
    name = "Test"
    description = "Runs the xunit suite across the solution. Mirrors the test job of ci.yml."

    buildNumberPattern = "%Version%"

    params {
        param("Version", "${SetVersion.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    steps {
        dotnetTest {
            name = "Test Solution"
            projects = "ConsequencesCore.slnx"
            configuration = "Release"
            args = "--nologo"
        }
    }

    dependencies {
        snapshot(SetVersion) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }

    requirements {
        contains("teamcity.agent.name", "windows")
        exists("DotNetCoreSDK9.0_Path")
    }
})


object Deploy : Project({
    name = "Deploy"

    buildType(Deploy_PushNuGets)
})

object Deploy_PushNuGets : BuildType({
    name = "Push NuGets"
    description = "Pushes both packages to the consequences-nuget-public Nexus feed using the NEXUS credentials inherited from the Consequences project."

    type = BuildTypeSettings.Type.DEPLOYMENT
    buildNumberPattern = "%Version%"

    params {
        param("nuget.source", "https://www.hec.usace.army.mil/nexus/repository/consequences-nuget-public/")
        param("Version", "${Build_Pack.depParamRefs["Version"]}")
        param("feed.name", "consequences-nuget-public")
    }

    vcs {
        root(Repo)
    }

    steps {
        powerShell {
            name = "Register Nexus feed credentials"
            scriptMode = script {
                content = """
                    ${'$'}ErrorActionPreference = 'Continue'
                    
                    # Remove first so a re-run does not fail on "source already exists"; the working
                    # copy's nuget.config is <clear/>ed and only has nuget.org, so this is a no-op on
                    # a clean checkout.
                    dotnet nuget remove source %feed.name% --configfile nuget.config 2>&1 | Out-Null
                    
                    ${'$'}ErrorActionPreference = 'Stop'
                    dotnet nuget add source "%nuget.source%index.json" `
                        --name %feed.name% `
                        --username "%env.NEXUS_USER%" `
                        --password "%env.NEXUS_PASSWORD%" `
                        --store-password-in-clear-text `
                        --configfile nuget.config
                    if (${'$'}LASTEXITCODE -ne 0) { throw "Failed to register the %feed.name% feed" }
                """.trimIndent()
            }
        }
        powerShell {
            name = "Push NuGets"
            scriptMode = script {
                content = """
                    ${'$'}ErrorActionPreference = 'Stop'
                    
                    # Credentials for %feed.name% were written into nuget.config by the previous step,
                    # so --source resolves by name and NuGet supplies basic auth from that entry.
                    # Deliberately NOT TeamCity's dotnet `nuget-push` runner: that runner carries an
                    # implicit agent requirement none of this server's Windows agents satisfy, which
                    # left the config with zero compatible agents.
                    ${'$'}packages = @(Get-ChildItem -Path "NuGets" -Filter "*.%Version%.nupkg")
                    if (${'$'}packages.Count -eq 0) { throw "No .nupkg files for version %Version% found under NuGets/" }
                    
                    # Belt and braces against the accumulation bug: refuse to publish anything that is
                    # not this chain's version, rather than silently shipping a leftover package.
                    ${'$'}stray = @(Get-ChildItem -Path "NuGets" -Filter *.nupkg |
                               Where-Object { ${'$'}_.Name -notlike "*.%Version%.nupkg" })
                    if (${'$'}stray.Count -gt 0) {
                        throw "Refusing to push; unexpected packages present: ${'$'}(${'$'}stray.Name -join ', ')"
                    }
                    
                    foreach (${'$'}p in ${'$'}packages) {
                        Write-Host "Pushing ${'$'}(${'$'}p.Name)"
                        dotnet nuget push ${'$'}p.FullName --source %feed.name% --skip-duplicate
                        if (${'$'}LASTEXITCODE -ne 0) { throw "Push failed for ${'$'}(${'$'}p.Name)" }
                    }
                    
                    Write-Host "Pushed ${'$'}(${'$'}packages.Count) package(s) at version %Version% to %feed.name%"
                """.trimIndent()
            }
        }
    }

    dependencies {
        dependency(Build_Pack) {
            snapshot {
                reuseBuilds = ReuseBuilds.NO
                onDependencyFailure = FailureAction.FAIL_TO_START
            }

            artifacts {
                cleanDestination = true
                artifactRules = "NuGets => NuGets"
            }
        }
    }

    requirements {
        contains("teamcity.agent.name", "windows")
        exists("DotNetCoreSDK9.0_Path")
    }
})


object Endpoints : Project({
    name = "Endpoints"

    buildType(Endpoints_PRReview)
    buildType(Endpoints_Release)
    buildType(Endpoints_Snapshot)
})

object Endpoints_PRReview : BuildType({
    name = "PR Review"
    description = "Pull requests: full build/sign/pack chain as a gate, with no publish."

    type = BuildTypeSettings.Type.DEPLOYMENT
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${Build_Pack.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    triggers {
        vcs {
            triggerRules = "-:.teamcity/**"
            branchFilter = "+:*/merge"
        }
    }

    dependencies {
        snapshot(Build_Pack) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }
})

object Endpoints_Release : BuildType({
    name = "Release"
    description = "v* tags: publishes the clean tag version to consequences-nuget-public."

    type = BuildTypeSettings.Type.DEPLOYMENT
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${Deploy_PushNuGets.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    triggers {
        vcs {
            triggerRules = "-:.teamcity/**"
            branchFilter = "+:v*"
        }
    }

    dependencies {
        snapshot(Deploy_PushNuGets) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }
})

object Endpoints_Snapshot : BuildType({
    name = "Snapshot"
    description = "Pushes to main: publishes %VersionBase%.<counter>-dev to consequences-nuget-public."

    type = BuildTypeSettings.Type.DEPLOYMENT
    buildNumberPattern = "%Version%"

    params {
        param("Version", "${Deploy_PushNuGets.depParamRefs["Version"]}")
    }

    vcs {
        root(Repo)
    }

    triggers {
        vcs {
            triggerRules = "-:.teamcity/**"
            branchFilter = "+:main"
        }
    }

    dependencies {
        snapshot(Deploy_PushNuGets) {
            reuseBuilds = ReuseBuilds.NO
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }
})
