# 0004. Hazard Provider Layer depends directly on RAS Geospatial

Date: 2026-07-02
Status: Accepted

## Context

The Hazard Provider Layer needs to read hydraulic modelling outputs (e.g., TIFFs) and be robust enough to handle multiple sources. RAS Geospatial already implements this raster-reading capability.

## Decision

The Hazard Provider Layer depends on RAS Geospatial directly rather than duplicating its geospatial/raster-reading capability.

## Consequences

Easier: avoids maintaining a second implementation of raster/TIFF reading; bug fixes and format support land in one place. Harder: Hazard Provider takes on a direct dependency on RAS Geospatial and is coupled to its release cadence and API.
