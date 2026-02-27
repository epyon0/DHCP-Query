using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DHCP_Query
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class ListViewColumnSorter : IComparer
    {
        private int columnToSort;
        private SortOrder orderOfSort;
        private CaseInsensitiveComparer objectCompare;

        public ListViewColumnSorter()
        {
            columnToSort = 0;
            orderOfSort = SortOrder.None;
            objectCompare = new CaseInsensitiveComparer();
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            string valueX = itemX.SubItems[columnToSort].Text;
            string valueY = itemY.SubItems[columnToSort].Text;

            int compareResult = objectCompare.Compare(valueX, valueY);

            if (orderOfSort == SortOrder.Ascending)
            {
                return compareResult;
            } else if (orderOfSort == SortOrder.Descending)
            {
                return -compareResult;
            } else
            {
                return 0;
            }
        }

        public int SortColumn
        {
            set { columnToSort = value; }
            get { return columnToSort; }
        }

        public SortOrder Order
        {
            set { orderOfSort = value; }
            get { return orderOfSort; }
        }
    }
}
