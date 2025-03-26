using Newtonsoft.Json;
using System.Text;
using PetaPoco;
using System.Data;
using MySql.Data.MySqlClient;
using Humanizer;
using AutoMapper;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClosetAPI.Models;
using APIHungryHunters.Models;

namespace SmartClosetAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContasController : ControllerBase
{
    private readonly TodoContext _contexto;
    private readonly IConfiguration _configuration;

    public ContasController(TodoContext contexto, IConfiguration configuration)
    {
        _contexto = contexto;
        _configuration = configuration;
    }

    // String de conexão com o banco de dados
    string conexaodb = "Server=localhost;Port=3306;Database=smartcloset;Uid=root;";

    [HttpGet("ListaDeContas")]
    public async Task<ActionResult<IEnumerable<ContasDTO>>> GetContas()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Contas, ContasDTO>();
        });
        AutoMapper.IMapper mapper = config.CreateMapper();

        using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
        {
            var contas = await db.FetchAsync<Contas>("SELECT * FROM user");

            var responseItems = mapper.Map<List<ContasDTO>>(contas);

            return Ok(responseItems);
        }
    }

    [HttpGet("ContasPor/{id}")]
    public async Task<ActionResult<IEnumerable<ContasDTO>>> GetContasPorId(long id)
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Contas, ContasDTO>();
        });
        AutoMapper.IMapper mapper = config.CreateMapper();

        using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
        {
            var conta = await db.FetchAsync<Contas>("SELECT * FROM user WHERE Id_conta = @0", id);

            if (conta == null)
            {
                return NotFound($"Não foi encontrada nenhuma Conta com o Id: {id}. Insira outro Id.");
            }

            var contasDTO = mapper.Map<List<ContasDTO>>(conta);

            return Ok(contasDTO);
        }
    }

    [HttpPost("DeleteContas")]
    public async Task<ActionResult> DeleteContas([FromBody] List<long> ids)
    {
        try
        {
            using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
            {
                foreach (var id in ids)
                {
                    var conta = await db.SingleOrDefaultAsync<Contas>("SELECT * FROM user WHERE Id_conta = @0", id);

                    if (conta == null)
                    {
                        return NotFound($"Não foi encontrada nenhuma conta com o Id: {id}. Insira outro Id.");
                    }
                    else
                    {
                        await db.DeleteAsync("user", "Id_conta", conta);
                    }
                }
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao excluir a conta");
        }
    }

    [HttpPost("AddConta")]
    public async Task<ActionResult> AddConta([FromBody] List<ContasDTO> ContasDTO)
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ContasDTO, Contas>();
        });
        AutoMapper.IMapper mapper = config.CreateMapper();

        using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
        {
            foreach (var contasDTO in ContasDTO)
            {
                var existingEmail = await db.FirstOrDefaultAsync<string>("SELECT Email FROM user WHERE Email = @Email", new { Email = contasDTO.Email });
                if (!string.IsNullOrEmpty(existingEmail))
                {
                    var erro1 = new { Mensagem = "Este email já está a ser utilizado."};
                    return BadRequest(erro1);
                }

                var existingUsername = await db.FirstOrDefaultAsync<string>("SELECT Username FROM user WHERE Username = @Username", new { Username = contasDTO.Username });
                if (!string.IsNullOrEmpty(existingUsername))
                {
                    var erro2 = new { Mensagem = "Este username já está a ser utilizado."};
                    return BadRequest(erro2);
                }

                if (string.IsNullOrWhiteSpace(contasDTO.Username) || contasDTO.Username.Length < 5 || contasDTO.Username.Length > 15 || contasDTO.Username.Contains(' '))
                {
                    var erro3 = new { Mensagem = "O username deve ter entre 5 e 15 caracteres e não deve conter espaços."};
                    return BadRequest(erro3);
                }

                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$");
                if (string.IsNullOrWhiteSpace(contasDTO.Email) || !emailRegex.IsMatch(contasDTO.Email))
                {
                    var erro4 = new { Mensagem = "Email inválido." };
                    return BadRequest(erro4);
                }
                var passwordregex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z])");

                if (string.IsNullOrWhiteSpace(contasDTO.Password) || contasDTO.Password.Length < 5 || contasDTO.Password.Length > 20 || contasDTO.Password.Contains(' ') || !passwordregex.IsMatch(contasDTO.Password))
                {
                    var erro6 = new { Mensagem = "A senha deve ter entre 5 e 20 caracteres, não deve conter espaços e deve conter letras maiúsculas, minúsculas, número e caractere especial." };
                    return BadRequest(erro6);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(contasDTO.Password);
                contasDTO.Password = hashedPassword;
                var novaconta = mapper.Map<Contas>(contasDTO);

                await db.InsertAsync("user", "Id_conta", true, novaconta);
            }
        }
        return Ok();
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
        {
            if(string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                var erro1 = new { Mensagem = "Campos obrigatórios" };
                return BadRequest(erro1);
            }
            var existingConta = await db.SingleOrDefaultAsync<Contas>("SELECT * FROM user WHERE Email = @0", loginDTO.Email);

            if (existingConta == null)
            {
                var naoautorizado = new { Mensagem = "Credenciais inválidasss" };
                return Unauthorized(naoautorizado);
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDTO.Password, existingConta.Password);

            if (!isPasswordValid)
            {
                var naoautorizado2 = new { Mensagem = "Credenciais inválidas" };
                return Unauthorized(naoautorizado2);
            }
        }
        var IdContas = ObterIdDoUtilizadorPorEmail(loginDTO.Email);
        var token = GenerateJwtToken(loginDTO.Email);
        return Ok(new { Token = token, Id = IdContas });
    }

    [HttpGet("ObterIdDoUtilizadorPorEmail")]
    public int? ObterIdDoUtilizadorPorEmail(string email)
    {
        using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
        {
            var utilizador = db.FirstOrDefault<Contas>("SELECT Id_conta FROM user WHERE Email = @0", email);

            return utilizador?.Id_conta;
        }
    }

    private string GenerateJwtToken(string email)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
        var signingCredentials = new SigningCredentials(
                                new SymmetricSecurityKey(key),
                                SecurityAlgorithms.HmacSha512Signature
                            );

        var subject = new ClaimsIdentity(new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
            });
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = subject,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = signingCredentials
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        return jwtToken;
    }

    [HttpPut("UpdateConta/{id}")]
    public async Task<ActionResult> UpdateConta(int id, [FromBody] ContasDTO contasDTO)
    {
        try
        {
            using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
            {
                var conta = await db.SingleOrDefaultAsync<Contas>("SELECT * FROM user WHERE Id_conta = @0", id);

                if (conta == null)
                {
                    return NotFound($"Não foi encontrada nenhuma conta com o Id: {id}. Insira outro Id.");
                }

                conta.Email = contasDTO.Email;
                conta.Username = contasDTO.Username;
                conta.Password = contasDTO.Password;

                await db.UpdateAsync("user", "Id_conta", conta);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar a conta");
        }
    }

	[HttpPut("ChangePassword")]
	public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
	{
		try
		{
			using (var db = new Database(conexaodb, "MySql.Data.MySqlClient"))
			{
				var conta = await db.SingleOrDefaultAsync<Contas>("SELECT * FROM user WHERE Email = @0", changePasswordDTO.Email);

				if (conta == null)
				{
					return NotFound($"Não foi encontrada nenhuma conta com o email: {changePasswordDTO.Email}. Insira outro email.");
				}
				string hashedPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDTO.NewPassword);
				conta.Password = hashedPassword;

				await db.UpdateAsync("user", "Id_conta", conta);
			}

			return NoContent();
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao atualizar a senha da conta");
		}
	}

	private bool ContasExist(long id)
    {
        return _contexto.Contas.Any(e => e.Id_conta == id);
    }
}