/* 
    Copyright ©  2020  Andrej Melekhin <QWERTYkez@outlook.com>.

    This file is part of FlexTrader
    FlexTrader is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    FlexTrader is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with FlexTrader. If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlexUserConrols
{
    public partial class XComboBox : UserControl
    {
        public XComboBox()
        {
            InitializeComponent();

            GRD.MouseDown += (s, e) => Pop.IsOpen = true;
            GRD.MouseLeave += ClosePopup;
            Pop.MouseLeave += ClosePopup;

            //this.ItemsSource = new ObservableCollection<XComboBoxElement>
            //{
            //    new XComboBoxElement("BTC", "Bitcoin"),
            //    new XComboBoxElement("ETH", "Etherium"),
            //    new XComboBoxElement("LTC", "Litecoin"),
            //    new XComboBoxElement("USDT", "Tether"),
            //    new XComboBoxElement("ALPHA", "Ravencoin")
            //};
        }

        private void ClosePopup(object sender, MouseEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                Dispatcher.Invoke(() =>
                {
                    if (!GRD.IsMouseOver && !Pop.IsMouseOver)
                        Pop.IsOpen = false;
                });
            });
        }

        public XComboBoxElement SelectedItem { get => (XComboBoxElement)LB.SelectedItem; set => LB.SelectedItem = value; }
        public int SelectedIndex { get => LB.SelectedIndex; set => LB.SelectedIndex = value; }
        private ObservableCollection<XComboBoxElement> Items;
        public ObservableCollection<XComboBoxElement> ItemsSource 
        { 
            get => Items; 
            set 
            {
                LB.ItemsSource = value;
                Items = value;
            }
        }
    }

    public class XComboBoxElement
    {
        public XComboBoxElement(string Big, string Small)
        {
            this.Big = Big;
            this.Small = Small;
        }

        public string Big { get; set; }
        public string Small { get; set; }
    }
}
