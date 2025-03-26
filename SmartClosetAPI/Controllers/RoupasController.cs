using APIHungryHunters.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartClosetAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using PetaPoco;
using System.Data;
using MySql.Data.MySqlClient;
using Humanizer;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SmartClosetAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RoupasController : Controller
	{
		private readonly TodoContext _contexto;
		private readonly IConfiguration _configuration;

		public RoupasController(TodoContext contexto, IConfiguration configuration)
		{
			_contexto = contexto;
			_configuration = configuration;
		}

		string conexaodb = "Server=localhost;Port=3306;Database=smartcloset;Uid=root;";

		[HttpGet("ListaDeRoupas")]
		public async Task<ActionResult<IEnumerable<RoupasDTO>>> GetRoupas()
		{
			var config = new MapperConfiguration(cfg =>
			{
				cfg.CreateMap<Roupas, RoupasDTO>();
			});
			AutoMapper.IMapper mapper = config.CreateMapper();

			using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
			{
				var roupas = await db.FetchAsync<Roupas>("SELECT * FROM roupas");

				var responseItems = mapper.Map<List<RoupasDTO>>(roupas);

				return Ok(responseItems);
			}
		}

		[HttpGet("RoupasPorId/{id}")]
		public async Task<ActionResult<IEnumerable<RoupasDTO>>> GetRoupasPorId(long id)
		{
			var config = new MapperConfiguration(cfg =>
			{
				cfg.CreateMap<Roupas, RoupasDTO>();
			});
			AutoMapper.IMapper mapper = config.CreateMapper();

			using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
			{
				var roupas = await db.FetchAsync<Roupas>("SELECT * FROM roupas WHERE Id_roupa = @0", id);

				if (roupas == null)
				{
					return NotFound($"Não foi encontrada nenhuma Roupa com o Id: {id}. Insira outro Id.");
				}

				var roupasDTO = mapper.Map<List<RoupasDTO>>(roupas);

				return Ok(roupasDTO);
			}
		}

		[HttpGet("RoupasporIdRoupa/")]
		public async Task<ActionResult<IEnumerable<RoupasDTO>>> RoupasporIdRoupa([FromQuery] string id)
		{
			var config = new MapperConfiguration(cfg =>
			{
				cfg.CreateMap<Roupas, RoupasDTO>();
			});
			AutoMapper.IMapper mapper = config.CreateMapper();

			using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
			{
				var roupas = await db.FetchAsync<Roupas>("SELECT * FROM roupas WHERE Id_conta = @0", id);

				if (roupas == null)
				{
					return NotFound($"Não foi encontrada nenhuma roupa com o Id: {id}. Insira outro Id.");
				}

				var roupasDTO = mapper.Map<List<RoupasDTO>>(roupas);

				return Ok(roupasDTO);
			}
		}


		[HttpPost("DeleteRoupas")]
		public async Task<ActionResult> DeleteRoupas([FromBody] List<long> ids)
		{
			try
			{
				using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
				{
					foreach (var id in ids)
					{
						var roupas = await db.SingleOrDefaultAsync<Roupas>("SELECT * FROM roupas WHERE Id_roupa = @0", id);

						if (roupas == null)
						{
							return NotFound($"Não foi encontrada nenhuma roupa com o Id: {id}. Insira outro Id.");
						}
						else
						{
							await db.DeleteAsync("roupas", "Id_roupa", roupas);
						}
					}
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao excluir a roupa");
			}
		}

		[HttpPost("AddRoupa")]
		public async Task<ActionResult> AddRoupa([FromBody] List<RoupasDTO> RoupasDTOs)
		{
			var config = new MapperConfiguration(cfg =>
			{
				cfg.CreateMap<RoupasDTO, Roupas>();
			});
			AutoMapper.IMapper mapper = config.CreateMapper();

			using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
			{
				foreach (var roupasDTO in RoupasDTOs)
				{
					var existingUsername = await db.FirstOrDefaultAsync<string>("SELECT nome FROM roupas WHERE nome = @nome", new { nome = roupasDTO.nome });

					if (string.IsNullOrWhiteSpace(roupasDTO.nome) || roupasDTO.nome.Length > 100)
					{
						var erro3 = new { Mensagem = "Passaste o limite de caracteres." };
						return BadRequest(erro3);
					}
					if (string.IsNullOrWhiteSpace(roupasDTO.marca) || roupasDTO.marca.Length > 100)
					{
						var erro3 = new { Mensagem = "Passaste o limite de caracteres." };
						return BadRequest(erro3);
					}
					if (string.IsNullOrWhiteSpace(roupasDTO.tamanho) || roupasDTO.tamanho.Length > 10)
					{
						var erro3 = new { Mensagem = "Passaste o limite de caracteres." };
						return BadRequest(erro3);
					}
					if (string.IsNullOrWhiteSpace(roupasDTO.cor) || roupasDTO.cor.Length > 50)
					{
						var erro3 = new { Mensagem = "Passaste o limite de caracteres." };
						return BadRequest(erro3);
					}
					if (string.IsNullOrWhiteSpace(roupasDTO.estado) || roupasDTO.estado.Length > 50)
					{
						var erro3 = new { Mensagem = "Passaste o limite de caracteres." };
						return BadRequest(erro3);
					}
					var roupas = await db.FirstOrDefaultAsync<string>("SELECT Id_conta FROM user WHERE Id_conta = @Id_conta", new { Id_conta = roupasDTO.Id_conta });
					if (string.IsNullOrEmpty(roupas))
					{
						return NotFound($"Não foi encontrada nenhuma Conta com este id: {roupasDTO.Id_conta}. Insira outro id.");
					}

					var novaRoupa = mapper.Map<Roupas>(roupasDTO);

					await db.InsertAsync("roupas", "Id_roupa", true, roupasDTO);
				}
			}
			return Ok();
		}

		[HttpPut("UpdateRoupa/{id}")]
		public async Task<ActionResult> UpdateRoupa(int id, [FromBody] RoupasDTO roupasDTO)
		{
			try
			{
				using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
				{
					var roupa = await db.SingleOrDefaultAsync<Roupas>("SELECT * FROM roupas WHERE Id_roupa = @0", id);

					if (roupa == null)
					{
						return NotFound($"Não foi encontrada nenhuma roupa com o Id: {id}. Insira outro Id.");
					}

					roupa.nome = roupasDTO.nome;
					roupa.marca = roupasDTO.marca;
					roupa.tamanho = roupasDTO.tamanho;
					roupa.cor = roupasDTO.cor;
					roupa.estado = roupasDTO.estado;

					await db.UpdateAsync("roupas", "Id_roupa", roupa);
				}

				return NoContent();
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar a roupa");
			}
		}

		private bool RoupasExist(long id)
		{
			return _contexto.Roupas.Any(e => e.Id_roupa == id);
		}
	}
}
