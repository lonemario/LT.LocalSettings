using System.ComponentModel.DataAnnotations;

namespace LT.LocalSettings.Models
{
    public class Setting
    {
        [Required]
        public string Name { get; set; }

        public string Value { get; set; }
    }
}
