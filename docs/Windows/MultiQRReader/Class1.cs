using System;
using System.Collections.Generic;
using ZXing;

namespace MultiQRReader
{
    public class Reader
    {
        public class ImageSplice
        {

        }


        public List<ImageSplice> SpliceImage()
        {
            List<ImageSplice> splices = new List<ImageSplice>();

            

            return splices;
        }

        public void TryRead(List<ImageSplice> splices)
        {
            // create a barcode reader instance
            IBarcodeReader reader = new BarcodeReader();
            // load a bitmap
            var barcodeBitmap = (Bitmap)Bitmap.LoadFrom("C:\\sample-barcode-image.png");
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(
            // do something with the result
            if (result != null)
            {
                txtDecoderType.Text = result.BarcodeFormat.ToString();
                txtDecoderContent.Text = result.Text;
            }
        }
    }
}
