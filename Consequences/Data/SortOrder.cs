using System;
using System.Xml.Serialization;

namespace Numerics.Data
{
    /// <summary>
    /// Enumeration defining the sort order of values.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    [Serializable]
    public enum SortOrder
    {
        /// <summary>
        /// Ascending order.
        /// </summary>
        [XmlEnum("Ascending")]
        Ascending,

        /// <summary>
        /// Descending order.
        /// </summary>
        [XmlEnum("Descending")]
        Descending,

        /// <summary>
        /// Not sorted. 
        /// </summary>
        [XmlEnum("None")]
        None
    }
}
