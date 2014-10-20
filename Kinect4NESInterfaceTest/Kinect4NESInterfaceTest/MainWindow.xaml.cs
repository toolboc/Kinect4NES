using Arduino4Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Kinect4NESInterfaceTest

{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Arduino board = new Arduino("COM3");

        Dictionary<string, int> NesButtons = new Dictionary<string, int>()
	    {
	        {"Right", 2},
	        {"Left", 3},
	        {"Down", 4},
	        {"Up", 5},
            {"Start", 6},
	        {"Select", 7},
	        {"B", 7},
	        {"A", 9}
	    };


        public MainWindow()
        {
            InitializePins();
            InitializeComponent();
        }

        private void InitializePins()
        {
            board.PinMode(2, PinMode.Output);
            board.PinMode(3, PinMode.Output);
            board.PinMode(4, PinMode.Output);
            board.PinMode(5, PinMode.Output);
            board.PinMode(6, PinMode.Output);
            board.PinMode(7, PinMode.Output);
            board.PinMode(8, PinMode.Output);
            board.PinMode(9, PinMode.Output);

            board.DigitalWrite(2, DigitalPin.High);
            board.DigitalWrite(3, DigitalPin.High);
            board.DigitalWrite(4, DigitalPin.High);
            board.DigitalWrite(5, DigitalPin.High);
            board.DigitalWrite(6, DigitalPin.High);
            board.DigitalWrite(7, DigitalPin.High);
            board.DigitalWrite(8, DigitalPin.High);
            board.DigitalWrite(9, DigitalPin.High);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            board.DigitalWrite(NesButtons[button.Content as string], DigitalPin.Low);
            Thread.Sleep(500);
            board.DigitalWrite(NesButtons[button.Content as string], DigitalPin.High);
        }

    }
}
