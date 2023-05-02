using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCart.Helpers
{
    public static class EmailBody
    {
        public static string EmailStringBody(string email, string emailToken)
        {
            return $@"<html>
            <head>
            </head>
            <body styles=""margin:0;padding:0;font-family: Arial, Helvetica; sars-serif;"">
             <div styles=""hieght: auto; background linear-gradient(to top,#c9c9ff 50%, #6e6ef6 90%) no-repeat;400px; padding:30px"">
              <div>
               <div>
                 <h1>Reset your Password</h1>
                 <hr>
                 <p>You're receiving this e-mail beacause you requested a password reset for you're account.</p>
                 
                 <p>Plaease tap the button below to choose a new password.</p>
                
                 <a href=""http://localhost:4200/reset?email={email}&code={emailToken}"" target"" _blank"" style=""background:#0d6efd; padding:10px; border:none;
                 color:white;border-radius:4px;disply:block;margin:0 auto;width:50%;text-align:center; text-decoration:none"">Reset Password</a><br>
                 

                 <p>Kind Regards</p>
               </div>
              </div>
             </div>
            </body>
            </html>"; 
        }
    }
}