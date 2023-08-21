using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lacuna.SignerService.Models
{
    public class CustomSigner
    {
        private string _cpf;
        public string CPF { get => _cpf;
            set { _cpf = Regex.Replace(value, @"[\.-]", ""); }
            }
        public string CustomText { get; set; }
    }
}
