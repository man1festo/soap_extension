using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Windows.Forms;

namespace MathService
{
    /// <summary>
    /// Сводное описание для Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Чтобы разрешить вызывать веб-службу из скрипта с помощью ASP.NET AJAX, раскомментируйте следующую строку. 
    // [System.Web.Script.Services.ScriptService]
    public class Service1 : System.Web.Services.WebService
    {
        string s;
        [WebMethod]
        public int Add(int a, int b)
        {
            s = "Параметры метода на сервере" + a.ToString() + "+" + b.ToString();
            MessageBox.Show(s);
            return (a + b);

        }

        [WebMethod]
        public System.Single Subtract(System.Single A, System.Single B)
        {
            return (A - B);
        }

        [WebMethod]
        public System.Single Multiply(System.Single A, System.Single B)
        {
            return A * B;
        }

        [WebMethod]
        public System.Single Divide(System.Single A, System.Single B)
        {
            if (B == 0)
                return -1;
            return Convert.ToSingle(A / B);
        }
					
    }
}