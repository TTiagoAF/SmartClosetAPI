using PetaPoco;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartClosetAPI.Models
{
	public class Roupas
	{
		[Key]
		public int Id_roupa { get; set; }
		public string nome { get; set; }
		public string marca { get; set; }
		public string tamanho { get; set; }
		public string cor { get; set; }
		public string estado { get; set; }
		public int Id_conta { get; set; }
		[ForeignKey("Id_conta")]
		[ResultColumn]
		public virtual Contas Contas { get; set; }
	}
}
