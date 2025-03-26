using PetaPoco;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartClosetAPI.Models
{
	public class RoupasDTO
	{
		[Key]
		public int Id_roupa { get; set; }
		public string nome { get; set; }
		public string marca { get; set; }
		public string tamanho { get; set; }
		public string cor { get; set; }
		public string estado { get; set; }
		public int Id_conta { get; set; }

	}
}
