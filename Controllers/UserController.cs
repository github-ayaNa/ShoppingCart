using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Registration.Helpers;
using ShoppingCart.Context;
using ShoppingCart.Models;
using System.Security.Cryptography;
using ShoppingCart.Helpers;
using ShoppingCart.UtilityService;
using ShoppingCart.Models.Dto;

namespace ShoppingCart.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext appDbContext,IConfiguration configuration,IEmailService emailService)
        {
            _authContext = appDbContext;
            _configuration = configuration;
            _emailService = emailService;

        }
        [HttpPost("Authenticate")]
        
         public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
          if(userObj == null)
              return BadRequest();
          var user = await _authContext.Users.FirstOrDefaultAsync(x=>x.Username == userObj.Username);
          if (user == null)
          
              return NotFound(new { Message = "User Not Found"});
          if(!PasswordHasher.VerifyPassword(userObj.Password,user.Password))
              return BadRequest(new { Message = "Password is Incorrect "});

            user.Token = CreateJWT(user); //method of creating token

            return Ok(new { Token = user.Token, Message = "Login Success!" }); //To access the token in other controllers too
          
        //   return Ok(new{ user,//Token = user.Token,
        //   Message = "Login Success!"});
          
        }

        // [HttpPost("LoginWithGoogle")]
        // public async Task<IActionResult> LoginWithGoogle([FromBody]string credentials)
        // {
        //     var settings = new GoogleJsonWebSignature.ValidationSettings()
        //     {
        //          Audience = new List<string> (this._configuration.ClientId)
        //     };
        //     return
        // }


        [HttpPost("register")]
        [AllowAnonymous]
        
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {

            
            if(userObj == null)
            return BadRequest();

            //check username
            if(await CheckUserNameExistAsync(userObj.Username))
                return BadRequest(new { Message = "Username Already Exist"});

            //check email
            if(await CheckEmailExistAsync(userObj.Email))
                return BadRequest(new { Message = "Email Already Exist"});

            //check password
            var pass = CheckPasswordStrength(userObj.Password);
            if(!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });
              



            userObj.Password = Registration.Helpers.PasswordHasher.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token = "";
            await _authContext.Users.AddAsync(userObj);
            await _authContext.SaveChangesAsync();
            return Ok(new { Message = "User Registered"});
        }
        private async Task<bool> CheckUserNameExistAsync(string username)
        {
            return  await _authContext.Users.AnyAsync(x => x.Username == username);
        }

        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return  await _authContext.Users.AnyAsync(x => x.Email == email);
        }

        private string CheckPasswordStrength(string password)
        {
           StringBuilder sb = new StringBuilder();
           if(password.Length < 8)
                sb.Append("Minimum password length should be 8" + Environment.NewLine);
           if(!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should be alphanuemeric" + Environment.NewLine);
           if(!Regex.IsMatch(password,"[<,>,!,@,#,$,%,^,&,*,(,),-,=,+,_,{,},',\\,.,;,:,,,~,?]"))
                sb.Append("Password should contain special chars" + Environment.NewLine);
            return sb.ToString();

        }
        private string CreateJWT(User user) //METHOD FOR CREATING TOKEN
        {
            var jWtTokenHandler = new JwtSecurityTokenHandler(); //JWKTOKENHANDLER CREATION
            var key = Encoding.ASCII.GetBytes("veryverysecret....."); //KEY
            var Identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role,user.Role),
                new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}")
            });           //Payload
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256); //credentials require key in bytes

            
            var tokenDescriptor = new SecurityTokenDescriptor //after all above we need to construct a token descriptor which requires the identity and the credentials
            {
                Subject = Identity,
                Expires = DateTime.Now.AddDays(7), //tokendesc also req the expiry of token
                SigningCredentials = credentials
            };
            var token = jWtTokenHandler.CreateToken(tokenDescriptor); //to create the token
            return jWtTokenHandler.WriteToken(token); //this helps to show the token as a string like "ehfhsdalkhaklklhfa.."
        }
        [Authorize]
        [HttpGet]

        public async Task<ActionResult<User>> GetAllUsers()
        {
            return  Ok(await _authContext.Users.ToListAsync());
        } 

        [HttpPost("send-reset-email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {
            var user = await _authContext.Users.FirstOrDefaultAsync(a => a.Email == email);
            if(user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Email Doesn't Exist"
                });
            }
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var emailToken = Convert.ToBase64String(tokenBytes);
            user.ResetPasswordToken = emailToken;
            user.ResetPasswordExpiry = DateTime.Now.AddMinutes(15);
            string from =  _configuration["EmailSettings:From"];
            var emailmodel = new EmailModel(email, "Reset Password!", EmailBody.EmailStringBody(email, emailToken));
            _emailService.SendEmail(emailmodel);
            _authContext.Entry(user).State = EntityState.Modified;
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "Email Sent!"
            });
         }   
         [HttpPost("reset-password")]
         public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
         {
            var newToken = resetPasswordDto.EmailToken.Replace(" ","+");
            var user = await _authContext.Users.AsNoTracking().FirstOrDefaultAsync(a => a.Email == resetPasswordDto.Email);
            if(user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "User Doesn't Exist"
                });
            }
            var tokenCode = user.ResetPasswordToken;
            DateTime emailTokenExpiry = user.ResetPasswordExpiry;
            if(tokenCode != resetPasswordDto.EmailToken || emailTokenExpiry < DateTime.Now)
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Invalid Reset link"
                });
            }
            user.Password = PasswordHasher.HashPassword(resetPasswordDto.NewPassword);
            _authContext.Entry(user).State = EntityState.Modified;
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                StateCode = 200,
                Message = "Password Reset Successfuly" 
            });
         }
    }
} 