using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Confuser.Runtime
{
    internal static class ResMut
    {
        internal static int Initialize(int i)
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            string x = assem.GetName().Name.ToString().Replace(" ", "_").Replace("-", "_");
            System.Resources.ResourceManager rman = new System.Resources.ResourceManager(x + ".Properties.Resources", assem);
            Stream ss = assem.GetManifestResourceStream(Mutation.Str1);
            string contents = new StreamReader(ss).ReadToEnd();
            string[] xD = contents.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            string[] miadama = xD[i].Split('^');
            return Convert.ToInt32(int.Parse(miadama[0]) ^ int.Parse(miadama[1]));
        }
    }
}
