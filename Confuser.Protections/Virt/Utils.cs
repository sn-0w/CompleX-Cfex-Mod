using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ROpCode = System.Reflection.Emit.OpCode;
using ROpCodes = System.Reflection.Emit.OpCodes;
using OperandType = dnlib.DotNet.Emit.OperandType;
using OpCode = dnlib.DotNet.Emit.OpCode;
using OpCodes = dnlib.DotNet.Emit.OpCodes;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;
using System.Reflection.Emit;
namespace Confuser.Protections.Virt
{
    class Utils
    {
        static Dictionary<OpCode, ROpCode> dnlibToReflection = new Dictionary<OpCode, ROpCode>();
        static ROpCode ropcode;
        public static ROpCode ConvertOpCode(OpCode opcode)
        {

            if (dnlibToReflection.TryGetValue(opcode, out ropcode))
                return ropcode;
            return ROpCodes.Nop;
        }
        public static void LoadOpCodes()
        {
            var refDict = new Dictionary<short, ROpCode>(0x100);
            foreach (var f in typeof(ROpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(ROpCode))
                    continue;
                var ropcode = (ROpCode)f.GetValue(null);
                refDict[ropcode.Value] = ropcode;
            }

            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(OpCode))
                    continue;
                var opcode = (OpCode)f.GetValue(null);
                if (!refDict.TryGetValue(opcode.Value, out ropcode))
                    continue;
                dnlibToReflection[opcode] = ropcode;
            }
        }
       
    }
    class Utils2
    {
        static Dictionary<ROpCode, OpCode> reflectionToDnlib = new Dictionary<ROpCode, OpCode>();
        static OpCode Opcode;
        public static OpCode ConvertOpCode(ROpCode ropcode)
        {

            if (reflectionToDnlib.TryGetValue(ropcode, out Opcode))
                return Opcode;
            return OpCodes.Nop;
        }
        public static void LoadOpCodes()
        {
            var refDict = new Dictionary<short, OpCode>(0x100);
            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(OpCode))
                    continue;
                var opcode = (OpCode)f.GetValue(null);
                refDict[opcode.Value] = opcode;
            }

            foreach (var f in typeof(ROpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (f.FieldType != typeof(ROpCode))
                    continue;
                var ropcode = (ROpCode)f.GetValue(null);
                if (!refDict.TryGetValue(ropcode.Value, out Opcode))
                    continue;
                reflectionToDnlib[ropcode] = Opcode;
            }
        }
    }
}
