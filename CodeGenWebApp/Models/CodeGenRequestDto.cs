using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CodeGenWebApp.Models
{
    /// <summary>
    /// The request object which contains swagger.json file.
    /// </summary>
    public class CodeGenRequestDto
    {
        /// <summary>
        /// The swagger.json file
        /// </summary>
        [Required]
        public IFormFile File { get; set; }

        /// <summary>
        /// The api prefix if any
        /// </summary>
        public string Prefix { get; set; }
    }
}
