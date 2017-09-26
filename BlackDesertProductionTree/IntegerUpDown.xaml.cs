using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlackDesertProductionTree
{
    /// <summary>
    /// IntegerUpDown.xaml 的交互逻辑
    /// </summary>
    public partial class IntegerUpDown : UserControl
    {
        public IntegerUpDown()
        {
            InitializeComponent();
            //ScrollBar_NUC.Maximum = IMaximum;
            //ScrollBar_NUC.Minimum = IMinimum;
            //ScrollBar_NUC.SmallChange = IIncrement;
        }

        public int IMaximum { get; set; } = 100;
        public int IMinimum { get; set; } = 0;
        public int IIncrement { get; set; } = 1;
        
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !AllNumeric(e.Text);
            base.OnPreviewTextInput(e);
        }
        private bool AllNumeric(string inp)
        {
            int tmp;
            return int.TryParse(inp, out tmp);
        }
        
        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //TB_Value.Text = ScrollBar_NUC.Value.ToString();
        }
    }
}
