using PetaPoco;
using System.ComponentModel.DataAnnotations;

namespace SmartClosetAPI.Models
{
    public class Contas
    {
        [Key]
        public int Id_conta { get; set; }

        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
		[ResultColumn]
		public List<Roupas> Roupas { get; set; }
	}
}
