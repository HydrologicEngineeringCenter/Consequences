using Consequences.Network.DTOs;

namespace Consequences.Network.Mapping;

/// <summary>
/// Projects a wire-format <see cref="NsiStructure"/> onto a domain receptor.
/// The DTO is the single JSON contract; the mapper is the seam that decides which
/// kind of building an import produces.
/// </summary>
public interface INsiStructureMapper<out TReceptor>
{
    TReceptor Map(NsiStructure structure);
}
