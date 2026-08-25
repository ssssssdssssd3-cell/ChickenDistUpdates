using System;
using System.Windows.Forms;
using ChickenDist.Forms;

namespace ChickenDist.Core
{
    public static class PrintHelper
    {
        public static void PrintSalePreparationSlip(int saleID)
        {
            try
            {
                new FrmPrintSale(saleID, "Prep", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء طباعة إذن التحضير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
