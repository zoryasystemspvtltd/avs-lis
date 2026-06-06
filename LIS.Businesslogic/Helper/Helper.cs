using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LIS.BusinessLogic.Helper
{
    public static class Helper
    {
        public static int GetAge(DateTime DateOfBirth)
        {
            DateTime zeroTime = new DateTime(1, 1, 1);

            TimeSpan span = DateTime.Now - DateOfBirth;

            int years = (zeroTime + span).Year - 1;

            return years;
        }

        public static string GeneratedBarcode(string barCodeValue, string annotationText)
        {
            throw new NotImplementedException();
            //string image = string.Empty;

            //try
            //{
            //    GeneratedBarcode barCode = IronBarCode.BarcodeWriter.CreateBarcode(barCodeValue, BarcodeWriterEncoding.Code128);
            //    //barCode.SetMargins(0, 10, 0, 10);
            //    barCode.AddAnnotationTextBelowBarcode(annotationText);
            //    barCode.ResizeTo(500, 35);
            //    image = ConvertToBase64(barCode.Image);
            //}
            //catch (Exception ex)
            //{
            //    throw (ex);
            //}

            //return image;
        }

        private static string ConvertToBase64(Image image)
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, ImageFormat.Png);
                byte[] imageBytes = m.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return "data:image/png;base64," + base64String;
            }
        }

        public static string GetSpecimen(string specimenName)
        {

            var specimen = string.Empty;

            if (string.IsNullOrWhiteSpace(specimenName))
            {
                specimen = "N";
                return specimen;
            }

            //NULL
            if (specimenName.ToUpper().Contains("NULL"))
            {
                specimen = "N";
                return specimen;
            }

            //24 hr urine
            //SPOT URINE
            //URINE
            //URINE.
            if (specimenName.ToUpper().Contains("URINE"))
            {
                specimen = "U";
                return specimen;
            }

            //BLOOD / URIN
            //BLOOD
            //BLOOD / FLUID
            //EDTA WHOLE BLOOD
            //HEPARINISED BLOOD
            //WHOLE BLOOD
            if (specimenName.ToUpper().Contains("BLOOD"))
            {
                specimen = "W";
                return specimen;
            }

            //C.S.F.
            if (specimenName.ToUpper().Contains("C.S.F"))
            {
                specimen = "C";
                return specimen;
            }

            //FLUID
            //FLUID.
            if (specimenName.ToUpper().Contains("FLUID"))
            {
                specimen = "F";
                return specimen;
            }
            
            //CITRATED PLASMA
            //PLASMA
            //PLASMA / SERUM
            if (specimenName.ToUpper().Contains("PLASMA"))
            {
                specimen = "P";
                return specimen;
            }
            //SEMEN
            if (specimenName.ToUpper().Contains("SEMEN"))
            {
                specimen = "E";
                return specimen;
            }
            //SERUM
            if (specimenName.ToUpper().Contains("SERUM"))
            {
                specimen = "S";
                return specimen;
            }
            //STONE
           
            if (specimenName.ToUpper().Contains("STONE"))
            {
                specimen = "T";
                return specimen;
            }
            //STOOL
            if (specimenName.ToUpper().Contains("STOOL"))
            {
                specimen = "L";
                return specimen;
            }

            return specimen;

        }

        public static string GetGroupTag(string froupName)
        {
            var groupTag = string.Empty;

            if (string.IsNullOrWhiteSpace(froupName))
            {
                groupTag = "N";
                return groupTag;
            }

            //CITR.PLAS
            if (froupName.ToUpper().Contains("CITR.PLAS"))
            {
                groupTag = "C";
                return groupTag;
            }

            //EDTA
            if (froupName.ToUpper().Contains("EDTA"))
            {
                groupTag = "E";
                return groupTag;
            }
            //F
            if (froupName.ToUpper().Contains("F"))
            {
                groupTag = "F";
                return groupTag;
            }
            //PLAIN
            if (froupName.ToUpper().Contains("PLAIN"))
            {
                groupTag = "G";
                return groupTag;
            }
            //PP
            if (froupName.ToUpper().Contains("PP"))
            {
                groupTag = "P";
                return groupTag;
            }
            //RAN
            if (froupName.ToUpper().Contains("RAN"))
            {
                groupTag = "R";
                return groupTag;
            }
            //URINE
            if (froupName.ToUpper().Contains("URINE"))
            {
                groupTag = "U";
                return groupTag;
            }

            return groupTag;
        }

        public static string GetGroupName(string sampleNumber)
        {
            var groupName = string.Empty;

            if (sampleNumber.ToUpper().EndsWith("N"))
            {
                groupName = string.Empty;
                return groupName;
            }

            //CITR.PLAS
            if (sampleNumber.ToUpper().EndsWith("C"))
            {
                groupName = "CITR.PLAS";
                return groupName;
            }

            //EDTA
            if (sampleNumber.ToUpper().EndsWith("E"))
            {
                groupName = "EDTA";
                return groupName;
            }
            //F
            if (sampleNumber.ToUpper().EndsWith("F"))
            {
                groupName = "F";
                return groupName;
            }
            //PLAIN
            if (sampleNumber.ToUpper().EndsWith("G"))
            {
                groupName = "PLAIN";
                return groupName;
            }
            //PP
            if (sampleNumber.ToUpper().EndsWith("P"))
            {
                groupName = "PP";
                return groupName;
            }
            //RAN
            if (sampleNumber.ToUpper().EndsWith("R"))
            {
                groupName = "RAN";
                return groupName;
            }
            //URINE
            if (sampleNumber.ToUpper().Contains("U"))
            {
                groupName = "URINE";
                return groupName;
            }

            return groupName;
        }
    }
}
