﻿using BookshopAPI.Models;
using BookshopAPI.Service;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.Extensions.Options;
using Google.Apis.Auth;
using Newtonsoft.Json.Linq;
using System.Management;
using static System.Net.WebRequestMethods;
using Microsoft.EntityFrameworkCore;
using BookshopAPI.Database;


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
        public async Task<IActionResult> getAll()
        {
            
            return Ok(responeMessage.response200(myDbContext.Users));
        }

        [HttpGet("getInfor")]
        [Authorize]
        public async Task<IActionResult> getInfor()
        {
            
            long userId = long.Parse(this.User.FindFirstValue("Id"));
            var user =await myDbContext.Users.SingleOrDefaultAsync(x => x.id == userId);
            return Ok(responeMessage.response200(user));
        }

        [HttpPost("changePassword")]
        [Authorize]
        public async Task<IActionResult> changePassword(string password)
        {
            long userId = long.Parse(this.User.FindFirstValue("Id"));
            var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.id == userId);
            user.password = Hash(password);
            await myDbContext.SaveChangesAsync();;
            return Ok(responeMessage.response200);
        }
        [HttpPost("changePasswordByAdmin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> changePasswordByAdmin(string username, string password)
        {
            
           var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.username == username);

            if (user != null)
            {
                user.password = Hash(password);
   
                await myDbContext.SaveChangesAsync();;
                return Ok(responeMessage.response200(null, "Đổi mật khẩu thành công"));
            }
            return Ok(responeMessage.response400);
           
           
        }

        [HttpPost("changeInfor")]
        [Authorize]
        public async Task<IActionResult> ChangeInfor(UserInfor userInfor)
        {
            long userId = long.Parse(this.User.FindFirstValue("Id"));
            var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.id == userId);
            user.fullName = userInfor.fullName;
            user.phoneNumber = userInfor.phoneNumber;
            user.email = userInfor.email;
            user.gender = userInfor.gender;
            await myDbContext.SaveChangesAsync();
            return Ok(responeMessage.response200(user));
        }
        [HttpPost("login")]
        public async Task<IActionResult> login(UserLogin userLogin) {
               var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.username == userLogin.username);
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
                    var accessToken =await generateToken(user);
                    return Ok(responeMessage.response200(accessToken, "Đăng nhập thành công")) ;
                }
            }
        }

        [HttpPost("sendOTP/email={email}")]
        public async Task<IActionResult> Otp(string email)
        {
            if (await myDbContext.Users.SingleOrDefaultAsync(x => x.email == email) != null)
            {
                var senMail = new SendMail();
                var rd = new Random();
                string otp = "";
                for (int i = 0; i < 6; i++)
                {
                    otp += rd.Next(0, 9);
                }
                senMail.SendEmail(email, otp);
                var sendOtp = await myDbContext.OPTs.SingleOrDefaultAsync(x => x.email == email);
                if(sendOtp != null)
                {
                    myDbContext.OPTs.Remove(sendOtp);
                    await myDbContext.SaveChangesAsync();;
                }
                sendOtp = new OTP
                {
                    email = email,
                    otp = otp,
                    accuracy = 0,
                    endAt = DateTime.Now.AddMinutes(5)
                };
                myDbContext.OPTs.Add(sendOtp);
                await myDbContext.SaveChangesAsync();;
                return Ok(responeMessage.response200(null, "Gửi OTP thành công"));
            }
            else
            {
                return Ok(responeMessage.response400(null, "Email chưa được đăng ký tài khoản"));
            }
        }
        [HttpPost("accuracyOTP")]
        public async Task<IActionResult> accuracyOtp(AccuracyOtp accuracyOtp)
        {
            var otp = await myDbContext.OPTs.SingleOrDefaultAsync(x => x.email == accuracyOtp.email);
            if (otp == null)
            {
                return Ok(responeMessage.response400(null, "Email không chính xác"));
            }
            else
            {
                if (otp.accuracy == 1) {
                    return Ok(responeMessage.response200);
                }
                else
                {
                    if (otp.endAt < DateTime.Now) {
                        return Ok(responeMessage.response400(null, "OTP đã hết hiệu lực"));
                    }
                    else
                    {
                        if (otp.otp != accuracyOtp.otp)
                        {
                            return Ok(responeMessage.response400(null, "OTP không chính xác"));
                        }
                        else
                        {
                            otp.accuracy = 1;
                            otp.endAt = DateTime.Now.AddMinutes(5);
                            await myDbContext.SaveChangesAsync();;
                            return Ok(responeMessage.response200(null, "Xác thực OTP thành công"));
                        }
                    }
                }
            }
        }
        [HttpPost("changePasswordByOTP")]
        public async Task<IActionResult> changePasswordByOTP(ChangePasswordOtp changePasswordOtp)
        {
            var otp = await myDbContext.OPTs.SingleOrDefaultAsync(x => x.email == changePasswordOtp.email);
            if(otp == null)
            {
                return Ok(responeMessage.response400(null, "Email không chính xác"));
            }
            else
            {
                if (otp.accuracy == 0)
                {
                    return Ok(responeMessage.response400(null, "OTP chưa được xác thực"));
                }
                else
                {
                   if(otp.endAt < DateTime.Now)
                    {
                        return Ok(responeMessage.response400(null, "OTP đã hết hạn"));
                    }
                    else
                    {
                        var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.email == otp.email);
                        user.password = Hash(changePasswordOtp.password);
                        myDbContext.OPTs.Remove(otp);
                        await myDbContext.SaveChangesAsync();;
                        return Ok(responeMessage.response200(null, "Đổi mật khẩu thành công"));
                    }
                }
            }
        }
        [HttpPost("register")]
        public async Task<IActionResult> register(UserRegister userRegister) {
            var user = await myDbContext.Users.SingleOrDefaultAsync(x => x.username == userRegister.username);
            if (user != null) {
                return BadRequest(responeMessage.response400("Username đã tồn tại!"));
            }else
            {
                user = myDbContext.Users.SingleOrDefault(x => x.email == userRegister.email);
                if (user != null)
                {
                    return Ok(responeMessage.response400("Email đã tồn tại!"));
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
                   await myDbContext.Users.AddAsync(user);
                    var rs =  await myDbContext.SaveChangesAsync();;
                    if (rs > 0)
                    {
                        var cart = new Cart
                        {
                            id = DateTime.Now.ToFileTimeUtc(),
                            userId = user.id,
                            createdAt = DateTime.Now

                        };
                        await myDbContext.Carts.AddAsync(cart);
                        await myDbContext.SaveChangesAsync();;
                        
                        return Ok(responeMessage.response200(user, "Đăng ký thành công"));
                    }
                    else
                    {
                        return Ok(responeMessage.response500);
                    }
                }
            }
            
        }
        [HttpPost("loginGoogleUser/token={token}")]
        public async Task<IActionResult> loginGoogle(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);

            var claim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "email");
            
            if (claim != null)
            {
                var email = claim?.Value;
                var fullName = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var user = myDbContext.Users.SingleOrDefault(x => x.email == email);
                if(user == null)
                {
                    user = new User
                    {
                        email = email,
                        fullName = fullName,
                        role = "CUSTOMER",
                        createAt = DateTime.Now,
                        gender = 1
                    };
                    await myDbContext.Users.AddAsync(user);
                    await myDbContext.SaveChangesAsync();

                }


                var accessToken = generateToken(user);
                return Ok(responeMessage.response200(accessToken, "Đăng nhập thành công"));
                
            }
            else
            {
                return Ok(responeMessage.response400("Token không chính xác!"));
            }

        }
        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {
            var refreshTK = await myDbContext.RefreshTokens.SingleOrDefaultAsync(x => x.refreshToken == refreshToken);
            if(refreshTK == null)
            {
                return Ok(responeMessage.response400(null, "RefreshToken không chính xác!"));
            }
            else
            {
                if(refreshTK.endAt < DateTime.Now)
                {
                    return Ok(responeMessage.response400(null, "RefreshToken đã hết hạn!"));
                }
                else
                {
                    User user = await myDbContext.Users.SingleOrDefaultAsync(x => x.id == refreshTK.userId);
                    LoginResponse loginResponse =await generateToken(user);
                    return Ok(responeMessage.response200(loginResponse));

                }
            }
            
        }
        private async Task<LoginResponse> generateToken(User user)
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


                     new Claim("Id", user.id+"")
                    
                    // role

                    //token
                    

                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKeyBytes), SecurityAlgorithms.HmacSha256Signature)

            };
            var token = jwtTokenHandler.CreateToken(tokenDesciption);
            var accessToken = jwtTokenHandler.WriteToken(token);
            var refreshTK = myDbContext.RefreshTokens.SingleOrDefault(x => x.userId == user.id);
            if (refreshTK == null)
            {
                refreshTK = new RefreshToken
                {
                    userId = user.id,
                    refreshToken = Guid.NewGuid().ToString()
                   
                };
                await myDbContext.RefreshTokens.AddAsync(refreshTK);

                await myDbContext.SaveChangesAsync();;
            }
            refreshTK.endAt = DateTime.UtcNow.AddDays(30);
            refreshTK.refreshToken = Guid.NewGuid().ToString();
            await myDbContext.SaveChangesAsync();;
            return new LoginResponse
            {
                accessToken = accessToken,
                refreshToken = refreshTK.refreshToken
            };
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
