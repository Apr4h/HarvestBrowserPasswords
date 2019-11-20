using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HarvestBrowserPasswords
{
    /// <summary>
    /// Takes a ByteArray and recursively parses TLV structure for a small range of ASN types.
    /// Minimal atm, solely for extracting/decrypting creds from Mozilla databases
    /// </summary>
    /// <param name="item2Bytes"></param>
    class ASN1Parser
    {
        //TODO: Bin this implementation and convert nested classes to json for more accurate parsing of the whole data structure

        //http://luca[.]ntop.org/Teaching/Appunti/asn1.html Section 2.3 Table 1 lists some ASN types
        enum ASN1Types
        {
            SEQUENCE = 0x30,
            OCTETSTRING = 4,
            OBJECTIDENTIFIER = 6,
            INTEGER = 2,
            NULL = 5
        }

        public ASN1Parser(byte[] item2Bytes)
        {
            this.Asn1ByteArray = item2Bytes;

            ParseTLV(0);
        }

        public bool finished = false;
        public byte[] Asn1ByteArray { get; set; }
        public byte[] EntrySalt { get; set; }
        public byte[] CipherText { get; set; }

        public void ParseTLV(int index)
        {
            //store the type value 
            int i = index;
            int type = (int)Asn1ByteArray[i];
            //and increment the index to the length field
            i += 1;

            //Check whether the length field is in short or long form then get length of the value
            int lengthForm = CheckLenthForm(Asn1ByteArray[i]); //Actually the number of octets used to represent the Length field
            int length = GetLength(i, lengthForm);

            //Increment the index to the value field
            i += lengthForm;

            //Check values
            if (type == (int)ASN1Types.SEQUENCE)
            {
                //DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DEBUG");
                Console.WriteLine("SEQUENCE FOUND");
                Console.WriteLine("INDEX = {0}", i - 2);
                Console.WriteLine("LENGTH = {0}", length);
                Console.WriteLine("LENGTHFORM = {0}", lengthForm);
                Console.ResetColor();
                int tempIndex = i;

                //Yay recursion
                ParseTLV(i);
                i += tempIndex + length;
            }
            else if (type == (int)ASN1Types.OBJECTIDENTIFIER)
            {
                byte[] tmpArray = new byte[length];

                for (int j = 0; j < length; j++)
                {
                    tmpArray[j] = Asn1ByteArray[i + j];
                }

                //DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DEBUG");
                Console.WriteLine("OBJECTIDENTIFIER FOUND");
                Console.WriteLine("INDEX = {0}", i - 2);
                Console.WriteLine("LENGTH = {0}", length);
                Console.WriteLine("LENGTHFORM = {0}", lengthForm);
                Console.WriteLine($"OBJECTID = {BitConverter.ToString(tmpArray)}");
                //Check that OID == 'pbeWithSha1AndTripleDES-CBC(3)'  
                i += length;

            }
            else if (type == (int)ASN1Types.OCTETSTRING)
            {
                //DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DEBUG");
                Console.WriteLine("OCTETSTRING FOUND");
                Console.WriteLine("INDEX = {0}", i - 2);
                Console.WriteLine("LENGTH = {0}", length);
                Console.WriteLine("LENGTHFORM = {0}", lengthForm);


                byte[] tmpArray = new byte[length];

                //Copy OCTETSTRING value into 
                for (int j = 0; j < length; j++)
                {
                    tmpArray[j] = Asn1ByteArray[i + j];
                }

                //Set entrySalt first, then encryptedValue
                if (EntrySalt == null)
                {
                    this.EntrySalt = tmpArray;

                    //DEBUG
                    Console.WriteLine($"ENTRYSALT = {BitConverter.ToString(EntrySalt)}");
                }
                else
                {
                    this.CipherText = tmpArray;

                    //DEBUG
                    Console.WriteLine($"ENCRYPTEDVALUE = {BitConverter.ToString(CipherText)}");
                    Console.ResetColor();
                }

                i += length;
            }
            else if (type == (int)ASN1Types.INTEGER)
            {
                //Don't care about these values. Move index to the start of the next TLV 
                i += length;
            }
            else if (type == (int)ASN1Types.NULL)
            {
                //Contents octets are empty. 
                //If BER encoded and length octets are in long form, increment 1 additional byte to next TLV. If DER encoded, index is already at next TLV
                if (lengthForm > 1)
                {
                    i += 1;
                }
            }
            else
            {
                //Some other type not accounted for in the ASN1Types enum
                i += length;
            }

            //Checked every type, meaning there was more than one element in this sequence. Move to next element
            //But first, check that we haven't hit the end of the ASN encoded data
            if (i < Asn1ByteArray.Length && finished == false)
            {
                ParseTLV(i);
            }
            else
            {
                finished = true;
            }
        }


        public int CheckLenthForm(byte length)
        {
            if ((length & 0x80) > 0) //Bit 8 of first octet has value 1 and bits 7-1 give number of additional length octets
            {
                //Long Form
                //Get number of additional length octets
                return (int)(length & 0x7f);
            }
            else
            {
                //Return 1 to indicate that the length field is stored in short form
                //Incrementing the index by the length form value will set the index to the start of the value field
                return 1;
            }
        }
        public int GetLength(int index, int lFormLength)
        {
            byte length = Asn1ByteArray[index];
            int longFormLength = lFormLength - 1;

            /*
            http://luca[.]ntop.org/Teaching/Appunti/asn1.html Chapter 3.1 describtes length octets of TL
            Check if length value is in long or short format
            */
            if (longFormLength > 1) //Set to 1 if length value is in short form
            {

                //DEBUG
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("DEBUG");
                Console.WriteLine("LONG FORM LENGTH TRIGGERED");
                Console.WriteLine($"INDEX = {index}");
                Console.WriteLine($"longFormLength = {longFormLength}");

                //Create new bytearray to store long form length value
                byte[] longFormBytes = new byte[longFormLength];

                //Copy length bytes from full byte array for conversion
                for (int i = 1; i < longFormLength + 1; i++)
                {
                    longFormBytes[i] = Asn1ByteArray[index + i];
                }

                try
                {
                    return BitConverter.ToInt32(longFormBytes, 0);
                }
                catch (Exception e)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Exception: {e}");
                    Console.ResetColor();
                    return (int)length;
                }
            }
            else
            {
                //Short Form
                return (int)length;
            }
        }
    }
}
