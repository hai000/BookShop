﻿using BookshopAPI.Models;
using BookshopAPI.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookshopAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private IConfiguration configuration = new MyDbContextService().GetConfiguration();
       private MyDbContext myDbContext = new MyDbContextService().GetMyDbContext();
        private ResponeMessage responeMessage = new ResponeMessage();

        [HttpGet("getAllUser")]
        [Authorize(Roles ="ADMIN")]
        public IActionResult getAll()
        {
            
            return Ok(this.User.FindFirstValue("UserName"));
        }
        [HttpPost("login")]
        public IActionResult login(UserLogin userLogin) {
               var user = myDbContext.Users.SingleOrDefault(x => x.username == userLogin.username);
            if (user == null)
            {
                return BadRequest(responeMessage.response400("Tài khoản không chính xác"));
            }
            else
            {
                if(user.password != Hash(userLogin.password))
                {
                    return BadRequest(responeMessage.response400("Mật khẩu không chính xác"));
                }
                else
                {
                    var accessToken = generateToken(user);
                    return Ok(responeMessage.response200(accessToken, "Đăng nhập thành công")) ;
                }
            }
        }
        [HttpPost("sendOTP/email={email}")]
        public IActionResult Otp(string email)
        {
            if(myDbContext.Users.SingleOrDefault(x => x.email == email) != null)
            {
                var senMail = new SendMail();
                var rd = new Random();
                string otp = "";
                for (int i = 0; i < 6; i++)
                {
                    otp += rd.Next(0, 9);
                }
                senMail.SendEmail(email, otp);
                var sendOtp = myDbContext.OPTs.SingleOrDefault(x => x.email == email);
                if(sendOtp != null)
                {
                    myDbContext.OPTs.Remove(sendOtp);
                    myDbContext.SaveChanges();
                }
                sendOtp = new OTP
                {
                    email = email,
                    otp = otp,
                    accuracy = 0,
                    endAt = DateTime.Now.AddMinutes(5)
                };
                myDbContext.OPTs.Add(sendOtp);
                myDbContext.SaveChanges();
                return Ok(responeMessage.response200("Gửi OTP thành công"));
            }
            else
            {
                return BadRequest(responeMessage.response400("Email chưa được đăng ký tài khoản"));
            }
        }
        [HttpPost("accuracyOTP")]
        public IActionResult accuracyOtp(AccuracyOtp accuracyOtp)
        {
            var otp = myDbContext.OPTs.SingleOrDefault(x => x.email == accuracyOtp.email);
            if (otp == null)
            {
                return BadRequest(responeMessage.response400("Email không chính xác"));
            }
            else
            {
                if (otp.accuracy == 1) {
                    return Ok(responeMessage.response200);
                }
                else
                {
                    if (otp.endAt < DateTime.Now) {
                        return BadRequest(responeMessage.response400("OTP đã hết hiệu lực"));
                    }
                    else
                    {
                        if (otp.otp != accuracyOtp.otp)
                        {
                            return BadRequest(responeMessage.response400("OTP không chính xác"));
                        }
                        else
                        {
                            otp.accuracy = 1;
                            otp.endAt = DateTime.Now.AddMinutes(5);
                            myDbContext.SaveChanges();
                            return Ok(responeMessage.response200("Xác thực OTP thành công"));
                        }
                    }
                }
            }
        }
        [HttpPost("changePasswordByOTP")]
        public IActionResult changePasswordByOTP(ChangePasswordOtp changePasswordOtp)
        {
            var otp = myDbContext.OPTs.SingleOrDefault(x => x.email ==  changePasswordOtp.email);
            if(otp == null)
            {
                return BadRequest(responeMessage.response400("Email không chính xác"));
            }
            else
            {
                if (otp.accuracy == 0)
                {
                    return BadRequest(responeMessage.response400("OTP chưa được xác thực"));
                }
                else
                {
                   if(otp.endAt < DateTime.Now)
                    {
                        return BadRequest(responeMessage.response400("OTP đã hết hạn"));
                    }
                    else
                    {
                        var user = myDbContext.Users.SingleOrDefault(x => x.email == otp.email);
                        user.password = Hash(changePasswordOtp.password);
                        myDbContext.OPTs.Remove(otp);
                        myDbContext.SaveChanges();
                        return Ok(responeMessage.response200("Đổi mật khẩu thành công"));
                    }
                }
            }
        }
        [HttpPost("register")]
        public IActionResult register(UserRegister userRegister) {
            var user = myDbContext.Users.SingleOrDefault(x => x.username == userRegister.username);
            if (user != null) {
                return BadRequest(responeMessage.response400("Username đã tồn tại!"));
            }else
            {
                user = myDbContext.Users.SingleOrDefault(x => x.email == userRegister.email);
                if (user != null)
                {
                    return BadRequest(responeMessage.response400("Email đã tồn tại!"));
                }
                else
                {
                    user = new User { id = DateTime.Now.ToFileTimeUtc(),
                        username = userRegister.username,
                        password = Hash(userRegister.password),
                        createAt = DateTime.Now,
                        fullName = userRegister.fullName, 
                        email = userRegister.email, 
                        role = "CUSTOMER" };
                    myDbContext.Users.Add(user);
                    var rs =  myDbContext.SaveChanges();
                    if (rs > 0)
                    {
                        var cart = new Cart
                        {
                            id = DateTime.Now.ToFileTimeUtc(),
                            userId = user.id,
                            createdAt = DateTime.Now

                        };
                        myDbContext.Carts.Add(cart);
                        myDbContext.SaveChanges();
                        
                        return Ok(responeMessage.response200(user, "Đăng ký thành công"));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, responeMessage.response500);
                    }
                }
            }
            
        }

        private String generateToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secretKeyBytes = Encoding.UTF8.GetBytes(configuration["AppSettings:SecretKey"]);
            var tokenDesciption = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.fullName),
                    new Claim(JwtRegisteredClaimNames.Email, user.email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, user.role),

                    new Claim("UserName", user.username),
                     new Claim("Id", user.id+""),
                    new Claim("Password", user.password)
                    // role

                    //token
                    

                }),
                Expires = DateTime.UtcNow.AddHours(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKeyBytes), SecurityAlgorithms.HmacSha256Signature)

            };
            var token = jwtTokenHandler.CreateToken(tokenDesciption);
            var accessToken = jwtTokenHandler.WriteToken(token);
            
            return accessToken;
        }
        public static string Hash(string s)
        {
            string hashed = "";
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("X2"));
                    }
                    hashed = sb.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return hashed;
        }
    }
}
