using System.ComponentModel.DataAnnotations;

namespace LT.LocalSettings.Models
{
    /// <summary>
    /// Setting Entity
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Setting Name [Unique key]
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Setting Value
        /// </summary>
        public string Value { get; set; }
    }
}
