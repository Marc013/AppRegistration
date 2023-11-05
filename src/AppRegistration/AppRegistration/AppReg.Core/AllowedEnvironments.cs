using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRegistration.AppReg.Core
{
    public class AllowedEnvironments
    {
        public static string GetAllowedEnvironments(string[] environment) 
        {
            var allowedEnvironments = new List<String>();

            for (int i = 0; i < environment.Length; i++)
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable($"ServicePrincipalSecret{environment[i]}")))
                {
                    allowedEnvironments.Add(environment[i]);
                }
            }

            return string.Join(", ", allowedEnvironments);
        }
    }
}
