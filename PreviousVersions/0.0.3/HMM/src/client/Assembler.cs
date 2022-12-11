using System;
using System.Collections.Generic;
using System.Text;

namespace HMM.Client
{
    class Assembler
    {
        StringBuilder linebuilder = new StringBuilder();
        public string errormessage = "";
        bool isbigendiandata = false;
        bool isbigendianinstr = false;
        int addresslength = 16;

        // Instruction Translation
        byte wbyte;
        int bitindex;
        List<byte> tokenoutput = new List<byte>();
        int byteoffset = 0;

        struct EnumStruct
        {
            public int nbits;
            public string name;
            public List<string> values;

            public string GetOpCode(string ivalue)
            {
                for(int i=0;i<values.Count;i++)
                {
                    if (ivalue == values[i])
                        return ""+i.ToString("X");
                }
                return "";
            }
        }

        struct InstrStruct
        {
            public string signiture;
            public string machinecode;
            public int length;
        }

        struct Operand
        {
            public string value;
            public int length;

            public Operand(string ivalue,int ilength)
            {
                value = ivalue;
                length = ilength;
            }
        }

        List<EnumStruct> enums = new List<EnumStruct>();
        List<InstrStruct> instructions = new List<InstrStruct>();

        Dictionary<string, int> enumdictionary = new Dictionary<string, int>();
        Dictionary<string, int> instrdictionary = new Dictionary<string, int>();
        Dictionary<string, int> labeldictionary = new Dictionary<string, int>();

        // Returns true if error
        public bool Assemble(string istr,byte[] idata)
        {
            errormessage = "";
            if (GetEnums(istr))
                return true;
            if (GetInstructions(istr))
                return true;
            if (CalculateLabelAddresses(istr))
                return true;
            if (AssembleProgram(istr,idata))
                return true;
            return false;
        }
        
        private bool GetEnums(string istr)
        {
            isbigendiandata = false;
            isbigendianinstr = false;
            addresslength = 16;
            enums.Clear();
            linebuilder.Clear();
            enumdictionary.Clear();
            // Find Enumerations section
            int enumsindex = istr.IndexOf(".config");
            if (enumsindex == -1)
            {
                errormessage = "AsmROM: Could not find '.config' section.";
                return true;
            }
            int index = enumsindex;
            int line = 0;
            index = GetLine(index, istr);
            if (index >= istr.Length)
            {
                errormessage = "AsmROM: Unexpected end of text at .config line " + line;
                return true;
            }
            line++;
            // Generate enumerations.
            int enumcount = 0;
            bool endofsection = false;
            while (!endofsection)
            {
                linebuilder.Clear();
                index = GetLine(index, istr);
                if (linebuilder.Length < 1 || linebuilder[0].Equals('.'))
                    endofsection = true;
                else
                {
                    // Split line
                    char[] separators = new char[] { ' ', ',' };
                    string[] tokens = linebuilder.ToString().Split(separators,StringSplitOptions.RemoveEmptyEntries);
                    if(tokens.Length>0)
                    {
                        // Check for config parameters
                        if (tokens[0].ToLower().Equals("BigData"))
                        {
                            isbigendiandata = true;
                        }
                        if (tokens[0].ToLower().Equals("BigInstruction"))
                        {
                            isbigendianinstr = true;
                        }
                        else if (tokens[0].ToLower().Equals("AddressLength"))
                        {
                            if (tokens.Length < 2)
                            {
                                errormessage = "AsmROM: address length missing value at .config line " + line + ".";
                                return true;
                            }
                            else
                            {
                                if(IsIntegerInvalid(tokens[1]))
                                {
                                    errormessage = "AsmROM: Invalid address length at .config line " + line + ".";
                                    return true;
                                }
                                else
                                {
                                    addresslength = Int32.Parse(tokens[1]);
                                }
                            }
                        }
                        else
                        {
                            EnumStruct enumstruct = new EnumStruct();
                            enumstruct.values = new List<string>();
                            enumstruct.nbits = -1;
                            if (IsNameInvalid(tokens[0]))
                            {
                                errormessage = "AsmROM: Enumeration name \"" + tokens[0] + "\" at .config line " + line + " is invalid.";
                                return true;
                            }
                            enumstruct.name = tokens[0];
                            int tokenindex = 1;
                            if (tokenindex >= tokens.Length)
                            {
                                errormessage = "AsmROM: Blank enumeration \"" + enumstruct.name + "\" at " + ".config line " + line + ".";
                                return true;
                            }
                            if (!IsIntegerInvalid(tokens[tokenindex]))
                            {
                                enumstruct.nbits = Int32.Parse(tokens[tokenindex]);
                                tokenindex++;
                                if (tokenindex >= tokens.Length)
                                {
                                    errormessage = "AsmROM: Blank enumeration \"" + enumstruct.name + "\" at " + ".config line " + line + ".";
                                    return true;
                                }
                            }
                            while (tokenindex < tokens.Length)
                            {
                                if (IsNameInvalid(tokens[tokenindex]))
                                {
                                    errormessage = "AsmROM: Enumeration value \"" + tokens[tokenindex] + "\" at " + ".config line " + line + " is invalid.";
                                    return true;
                                }
                                enumstruct.values.Add(tokens[tokenindex]);
                                tokenindex++;
                            }
                            if (enumstruct.nbits < 0)
                            {
                                enumstruct.nbits = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(enumstruct.values.Count, 2));
                            }
                            enums.Add(enumstruct);
                            for(int i=0;i<enumstruct.values.Count;i++)
                            {
                                if(enumdictionary.ContainsKey(enumstruct.values[i]))
                                {
                                    errormessage = "AsmROM: Duplicate enumeration value \"" + enumstruct.values[i] + "\" at " + ".config line " + line + ".";
                                    return true;
                                }
                                enumdictionary.Add(enumstruct.values[i], enumcount);
                            }
                            enumcount++;
                        }
                    }
                }
                line++;
            }
            return false;
        }

        private bool GetInstructions(string istr)
        {
            instructions.Clear();
            instrdictionary.Clear();
            linebuilder.Clear();
            // Find Instructions section
            int instrindex = istr.IndexOf(".instr");
            if (instrindex == -1)
            {
                errormessage = "AsmROM: Could not find '.instr' section.";
                return true;
            }
            int index = instrindex;
            int line = 0;
            index = GetLine(index, istr);
            if (index >= istr.Length)
            {
                errormessage = "AsmROM: Unexpected end of text at .instr line " + line;
                return true;
            }
            line++;
            bool endofsection = false;
            int opcount = 0;
            int instrcount = -1;
            List<int> oplengths = new List<int>();
            InstrStruct istruct = new InstrStruct();
            istruct.signiture = "";
            istruct.machinecode = "";
            istruct.length = 0;
            while (!endofsection)
            {
                linebuilder.Clear();
                index = GetLine(index, istr);
                if (linebuilder.Length < 1 || linebuilder[0].Equals('.'))
                    endofsection = true;
                else
                {
                    // Split line
                    char[] separators = new char[] { ' ', ',' };
                    string[] tokens = linebuilder.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0)
                    {
                        if (!IsNameInvalid(tokens[0]))
                        {
                            if(instrcount>=0)
                            {
                                if(instrdictionary.ContainsKey(istruct.signiture))
                                {
                                    errormessage = "AsmROM: Duplicate instruction signiture at .instr line " + line + ".";
                                    return true;
                                }
                                instructions.Add(istruct);
                                instrdictionary.Add(istruct.signiture, instrcount);
                            }
                            instrcount++;
                            istruct = new InstrStruct();
                            istruct.signiture = "";
                            istruct.machinecode = "";
                            istruct.length = 0;
                            opcount = 0;
                            oplengths.Clear();
                            // Instruction Signiture line
                            istruct.signiture += tokens[0];
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                bool foundenum = false;
                                for(int j=0;j<enums.Count;j++)
                                {
                                    if (enums[j].name == tokens[i])
                                    {
                                        foundenum = true;
                                        oplengths.Add(enums[j].nbits);
                                        break;
                                    }
                                }
                                if (foundenum)
                                {
                                    istruct.signiture += "," + tokens[i];
                                }
                                else if (tokens[i].Equals("im8"))
                                {
                                    istruct.signiture += "," + tokens[i];
                                    oplengths.Add(8);
                                }
                                else if (tokens[i].Equals("im16"))
                                {
                                    istruct.signiture += "," + tokens[i];
                                    oplengths.Add(16);
                                }
                                else if (tokens[i].Equals("im32"))
                                {
                                    istruct.signiture += "," + tokens[i];
                                    oplengths.Add(32);
                                }
                                else if (tokens[i].Equals("im64"))
                                {
                                    istruct.signiture += "," + tokens[i];
                                    oplengths.Add(64);
                                }
                                else
                                {
                                    errormessage = "AsmROM: Unrecognized operand type \"" + tokens[i] + "\" at .instr line " + line + ".";
                                    return true;
                                }
                                opcount++;
                            }
                        }
                        else if(tokens[0][0].Equals(':'))
                        {
                            if(instrcount < 0)
                            {
                                errormessage = "AsmROM: Expected Instruction signiture at .instr line " + line + ".";
                                return true;
                            }
                            // Machine code line
                            tokens[0] = tokens[0].Substring(1);
                            int mclength = 0;
                            if((mclength = IsMachineCodeInvalid(tokens[0],oplengths)) < 0)
                            {
                                errormessage = "AsmROM: Machine code \"" + tokens[0] + "\" at .instr line " + line + " is invalid.";
                                return true;
                            }
                            istruct.length += UnityEngine.Mathf.CeilToInt(mclength / 8.0f);
                            istruct.machinecode += tokens[0];
                            for(int i=1;i<tokens.Length;i++)
                            {
                                if ((mclength = IsMachineCodeInvalid(tokens[i], oplengths)) < 0)
                                {
                                    errormessage = "AsmROM: Machine code \"" + tokens[i] + "\" at .instr line " + line + " is invalid.";
                                    return true;
                                }
                                istruct.length += UnityEngine.Mathf.CeilToInt(mclength / 8.0f);
                                istruct.machinecode += "," + tokens[i];
                            }
                            istruct.machinecode += "\n";
                        }
                        else
                        {
                            errormessage = "AsmROM: Instruction name or machine code \"" + tokens[0] + "\" at .instr line " + line + " is invalid.";
                            return true;
                        }
                    }
                }
                line++;
            }
            if(instrcount>=0)
            {
                if (instrdictionary.ContainsKey(istruct.signiture))
                {
                    errormessage = "AsmROM: Duplicate instruction signiture at .instr line " + line + ".";
                    return true;
                }
                instructions.Add(istruct);
                instrdictionary.Add(istruct.signiture, instrcount);
            }
            return false;
        }

        private bool CalculateLabelAddresses(string istr)
        {
            labeldictionary.Clear();
            linebuilder.Clear();
            // Find Program section
            int progindex = istr.IndexOf(".prog");
            if (progindex == -1)
            {
                errormessage = "AsmROM: Could not find '.prog' section.";
                return true;
            }
            int index = progindex;
            int line = 0;
            index = GetLine(index, istr);
            if (index >= istr.Length)
            {
                errormessage = "AsmROM: Unexpected end of text at .prog line " + line;
                return true;
            }
            line++;
            byteoffset = 0;
            bool endofsection = false;
            while (!endofsection)
            {
                linebuilder.Clear();
                index = GetLine(index, istr);
                if (linebuilder.Length < 1 || linebuilder[0].Equals('.'))
                    endofsection = true;
                else
                {
                    // Split line
                    char[] separators = new char[] { ' ', ',' };
                    string[] tokens = linebuilder.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0)
                    {
                        int ti = 0;
                        if (tokens[ti].EndsWith(":"))         // Label
                        {
                            tokens[ti] = tokens[ti].Substring(0, tokens[ti].Length - 1);
                            if (IsNameInvalid(tokens[ti]))
                            {
                                errormessage = "AsmROM: Invalid Label \"" + tokens[ti] + "\" at .prog line " + line + ".";
                                return true;
                            }
                            if (labeldictionary.ContainsKey(tokens[ti]))
                            {
                                errormessage = "AsmROM: Duplicate Label \"" + tokens[ti] + "\" at .prog line " + line + ".";
                                return true;
                            }
                            labeldictionary.Add(tokens[ti], byteoffset);
                            ti++;
                        }
                        if(tokens.Length > ti)    // Instruction
                        {
                            if (tokens[ti].StartsWith("0x"))  // Data
                            {
                                tokens[ti] = tokens[ti].Substring(2);
                                if (IsHexInvalid(tokens[ti]))
                                {
                                    errormessage = "AsmROM: Invalid Hex at .prog line " + line + ".";
                                    return true;
                                }
                                byteoffset += UnityEngine.Mathf.CeilToInt(tokens[ti].Length / 2.0f);
                            }
                            else       // Instruction
                            {
                                string sig = tokens[ti];
                                for (int i = ti + 1; i < tokens.Length; i++)
                                {
                                    if (enumdictionary.ContainsKey(tokens[i]))
                                    {
                                        EnumStruct tenum = enums[enumdictionary[tokens[i]]];
                                        sig += "," + tenum.name;
                                    }
                                    else if (tokens[i].StartsWith("0x"))
                                    {
                                        tokens[i] = tokens[i].Substring(2);
                                        if (IsHexInvalid(tokens[i]))
                                        {
                                            errormessage = "AsmROM: Invalid Hex at .prog line " + line + ".";
                                            return true;
                                        }
                                        int nbits = UnityEngine.Mathf.CeilToInt(tokens[i].Length / 2.0f) * 8;
                                        sig += ",im" + nbits;
                                    }
                                    else
                                    {
                                        if (IsNameInvalid(tokens[i]))
                                        {
                                            errormessage = "AsmROM: Invalid operand \"" + tokens[i] + "\" at .prog line " + line + ".";
                                            return true;
                                        }
                                        int nbits = UnityEngine.Mathf.CeilToInt(addresslength / 8.0f) * 8;
                                        sig += ",im" + nbits;
                                    }
                                }
                                if (!instrdictionary.ContainsKey(sig))
                                {
                                    errormessage = "AsmROM: Could not find instruction with signiture " + sig + " at .prog line " + line + ".";
                                    return true;
                                }
                                else
                                {
                                    InstrStruct tinstr = instructions[instrdictionary[sig]];
                                    byteoffset += tinstr.length;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool AssembleProgram(string istr, byte[] idata)
        {
            linebuilder.Clear();
            // Find Program section
            int progindex = istr.IndexOf(".prog");
            if (progindex == -1)
            {
                errormessage = "AsmROM: Could not find '.prog' section.";
                return true;
            }
            int index = progindex;
            int line = 0;
            index = GetLine(index, istr);
            if (index >= istr.Length)
            {
                errormessage = "AsmROM: Unexpected end of text at .prog line " + line;
                return true;
            }
            line++;
            List<Operand> operands = new List<Operand>();
            byteoffset = 0;
            bool endofsection = false;
            while (!endofsection)
            {
                linebuilder.Clear();
                index = GetLine(index, istr);
                if (linebuilder.Length < 1 || linebuilder[0].Equals('.'))
                    endofsection = true;
                else
                {
                    // Split line
                    char[] separators = new char[] { ' ', ',' };
                    string[] tokens = linebuilder.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length > 0)
                    {
                        int ti = 0;
                        if(tokens[ti].EndsWith(":"))         // Label
                        {
                            ti++;
                        }
                        if(tokens.Length > ti)    // Instruction
                        {
                            if (tokens[ti].StartsWith("0x"))     // Data
                            {
                                tokens[ti] = tokens[ti].Substring(2);
                                if (IsHexInvalid(tokens[ti]))
                                {
                                    errormessage = "AsmROM: Invalid Hex at .prog line " + line + ".";
                                    return true;
                                }
                                if (TranslateHex(tokens[ti], idata))
                                    return true;
                            }
                            else               // Instruction
                            {
                                operands.Clear();
                                string sig = tokens[ti];
                                for (int i = ti + 1; i < tokens.Length; i++)
                                {
                                    if (enumdictionary.ContainsKey(tokens[i]))
                                    {
                                        EnumStruct tenum = enums[enumdictionary[tokens[i]]];
                                        sig += "," + tenum.name;
                                        operands.Add(new Operand(tenum.GetOpCode(tokens[i]), tenum.nbits));
                                    }
                                    else if (tokens[i].StartsWith("0x"))
                                    {
                                        tokens[i] = tokens[i].Substring(2);
                                        if (IsHexInvalid(tokens[i]))
                                        {
                                            errormessage = "AsmROM: Invalid Hex at .prog line " + line + ".";
                                            return true;
                                        }
                                        int nbits = UnityEngine.Mathf.CeilToInt(tokens[i].Length / 2.0f) * 8;
                                        sig += ",im" + nbits;
                                        operands.Add(new Operand(tokens[i], nbits));
                                    }
                                    else
                                    {
                                        if (IsNameInvalid(tokens[i]) || !labeldictionary.ContainsKey(tokens[i]))
                                        {
                                            errormessage = "AsmROM: Invalid operand \"" + tokens[i] + "\" at .prog line " + line + ".";
                                            return true;
                                        }
                                        int nbits = UnityEngine.Mathf.CeilToInt(addresslength / 8.0f) * 8;
                                        sig += ",im" + nbits;
                                        operands.Add(new Operand(labeldictionary[tokens[i]].ToString("X"), nbits));
                                    }
                                }
                                if (!instrdictionary.ContainsKey(sig))
                                {
                                    errormessage = "AsmROM: Could not find instruction with signiture " + sig + " at .prog line " + line + ".";
                                    return true;
                                }
                                else
                                {
                                    InstrStruct tinstr = instructions[instrdictionary[sig]];
                                    if (TranslateInstruction(tinstr.machinecode, operands, idata))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool TranslateHex(string istr,byte[] idata)
        {
            tokenoutput.Clear();
            wbyte = 0;
            bitindex = 0;
            int number = 0;
            for (int i = istr.Length - 1; i >= 0; i--)
            {
                number = 0;
                int.TryParse(istr.Substring(i, 1), System.Globalization.NumberStyles.HexNumber, null, out number);
                for(int j=0;j<4;j++)
                {
                    if ((number & (1 << j)) > 0)
                    {
                        wbyte = (byte)(wbyte | 1 << bitindex);
                    }
                    IncBitIndex();
                }
            }
            if (bitindex != 0)
                tokenoutput.Add(wbyte);
            if (isbigendiandata)
                tokenoutput.Reverse();
            for (int i = 0; i < tokenoutput.Count; i++)
            {
                if (byteoffset + i >= idata.Length)
                {
                    errormessage = "AsmROM: Assembled program too large.";
                    return true;
                }
                idata[byteoffset + i] = tokenoutput[i];
            }
            byteoffset += tokenoutput.Count;
            return false;
        }
        
        private bool TranslateInstruction(string imachinecode,List<Operand> ioperands,byte[] idata)
        {
            List<byte> output = new List<byte>();
            char[] splitchars = new char[] { '\n' };
            string[] lines = imachinecode.Split(splitchars, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                splitchars = new char[] { ' ', ',' };
                string[] tokens = lines[i].Split(splitchars, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < tokens.Length; j++)
                {
                    wbyte = 0;
                    bitindex = 0;
                    tokenoutput.Clear();
                    for (int k = tokens[j].Length - 1; k >= 0; k--)
                    {
                        if (tokens[j][k].Equals('0'))
                            IncBitIndex();
                        else if (tokens[j][k].Equals('1'))
                        {
                            wbyte = (byte)(wbyte | (1 << bitindex));
                            IncBitIndex();
                        }
                        else  // Translate Operand
                        {
                            if (!(k - 1 >= 0 && tokens[j][k] == tokens[j][k - 1]))
                            {
                                int opindex = tokens[j][k] - 0x61;
                                int number = 0;
                                int nibbleindex = ioperands[opindex].value.Length-1;
                                if (nibbleindex >= 0)
                                    int.TryParse(ioperands[opindex].value.Substring(nibbleindex, 1), System.Globalization.NumberStyles.HexNumber, null, out number);
                                int opbit = 0;
                                while (opbit < ioperands[opindex].length)
                                {
                                    if ((number & (1 << (opbit % 4))) > 0)
                                    {
                                        wbyte = (byte)(wbyte | 1 << bitindex);
                                    }
                                    IncBitIndex();
                                    opbit++;
                                    if (opbit % 4 == 0)
                                    {
                                        nibbleindex--;
                                        number = 0;
                                        if (nibbleindex >= 0)
                                            int.TryParse(ioperands[opindex].value.Substring(nibbleindex, 1), System.Globalization.NumberStyles.HexNumber, null, out number);
                                    }
                                }
                            }
                        }
                    }
                    if(bitindex!=0)
                        tokenoutput.Add(wbyte);
                    if (j == 0 && isbigendianinstr)
                        tokenoutput.Reverse();
                    else if (j > 0 && isbigendiandata)
                        tokenoutput.Reverse();
                    for(int k=0;k<tokenoutput.Count;k++)
                    {
                        if (byteoffset + k >= idata.Length)
                        {
                            errormessage = "AsmROM: Assembled program too large.";
                            return true;
                        }
                        idata[byteoffset + k] = tokenoutput[k];
                    }
                    byteoffset += tokenoutput.Count;
                }
            }
            return false;
        }

        private void IncBitIndex()
        {
            bitindex++;
            if (bitindex > 7)
            {
                tokenoutput.Add(wbyte);
                wbyte = 0;
                bitindex = 0;
            }
        }

        private int GetLine(int start,string istr)
        {
            while (start < istr.Length && istr[start] != '\n' && istr[start] != '#')
            {
                linebuilder.Append(istr[start]);
                start++;
            }
            while (start < istr.Length && istr[start] != '\n')
            {
                start++;
            }
            start++;
            return start;
        }

        private bool IsNameInvalid(string istr)
        {
            if (istr.Length < 1)
                return true;
            if (!(Char.IsLetter(istr[0]) || istr[0] == '_'))
                return true;
            for(int i=0;i<istr.Length;i++)
            {
                if (!(char.IsLetterOrDigit(istr[i]) || istr[i] == '_'))
                    return true;
            }
            return false;
        }

        private bool IsIntegerInvalid(string istr)
        {
            for (int i = 0; i < istr.Length; i++)
            {
                if (!char.IsDigit(istr[i]))
                    return true;
            }
            return false;
        }

        private int IsMachineCodeInvalid(string istr, List<int> ioplengths)
        {
            int mclength = 0;
            for (int i = 0; i < istr.Length; i++)
            {
                int chari = istr[i] - 0x61;
                if(!(istr[i]=='0'||istr[i]=='1'||(chari>=0&&chari< ioplengths.Count)))
                {
                    return -1;
                }
                if (istr[i] == '0' || istr[i] == '1')
                {
                    mclength++;
                }
                else if ((chari >= 0 && chari < ioplengths.Count))
                {
                    if(!(i+1 < istr.Length && istr[i]==istr[i+1]))
                        mclength += ioplengths[chari];
                }
                else
                    return -1;
            }
            return mclength;
        }

        private bool IsHexInvalid(string istr)
        {
            for (int i = 0; i < istr.Length; i++)
            {
                int chari = Char.ToLower(istr[i]) - 0x61;
                if (!(char.IsDigit(istr[i]) || (chari >= 0 && chari < 6)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
