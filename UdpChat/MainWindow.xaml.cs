using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;



namespace UdpChat
{
    public partial class MainWindow : Window
    {
        bool alive = false; // будет ли работать поток для приема

        const int TTL = 20;
        const int localPort = 8001; //порт для принятия сообщений
        const int remotePort = 8001; //порт для отправки сообщений
        const string Host = "235.5.5.1"; // хост для групповой рассылки
        IPAddress groupAdress; // адрес для групповой рассылки

        UserClient uClient; //обьект клиента

        public MainWindow()
        {
            InitializeComponent();
            logOutButton.IsEnabled = false;
            loginButton.IsEnabled = true;
            sendButton.IsEnabled = false;

            groupAdress = IPAddress.Parse(Host);
        }

        // обработчик нажатия кнопки loginButton
        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            if (userNameTextBox.Text != "")
            {
                //userName = userNameTextBox.Text;
                
                userNameTextBox.IsReadOnly = true;
                try
                {
                    //client = new UdpClient(localPort);

                    uClient = new UserClient(userNameTextBox.Text, localPort);
                    // присоединяемся к групповой рассылке
                    uClient.Client.JoinMulticastGroup(groupAdress, TTL);
                    //client.JoinMulticastGroup(groupAdress, TTL);

                    // запускаем задачу на прием сообщений
                    Task task = new Task(ReceiveMessage);
                    task.Start();

                    // отправляем первое сообщение о входе нового пользователя
                    string message = uClient.UserName + " - вошел в чат!";
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    uClient.Client.Send(data, data.Length, Host, remotePort);

                    //отключаем и включаем элементы управления
                    loginButton.IsEnabled = false;
                    logOutButton.IsEnabled = true;
                    sendButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    userNameTextBox.IsReadOnly = false;
                }
            }
            else
                MessageBox.Show("Введите ваше имя!");
        }

        // обработчик нажатия кнопки отправки сообщения
        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (messageTextBox.Text != "")
                {
                    string message = String.Format("{0}: {1}", uClient.UserName, messageTextBox.Text);
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    uClient.Client.Send(data, data.Length, Host, remotePort);
                    messageTextBox.Clear();
                }
                else
                    MessageBox.Show("Введите сообщение!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }          
        }

        // обработчик нажатия кнопки logoutButton
        private void logOutButton_Click(object sender, RoutedEventArgs e)
        {
            ExitChat();
        }

        // метод приема сообщений
        private void ReceiveMessage()
        {
            alive = true;

            try
            {
                while (alive)
                {
                    IPEndPoint remoteIP = null;
                    byte[] data = uClient.Client.Receive(ref remoteIP);
                    string message = Encoding.Unicode.GetString(data);

                    // добавляем полученное сообщение в текстовое поле
                    //поскольку мы обращаемся из другого потока то нужно использовать метод Invoke
                    Action action = () =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.Text = time + " " + message + "\r\n" + chatTextBox.Text;
                    };
                    Dispatcher.Invoke(action);
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                {
                    return;
                }
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // выход из чата
        private void ExitChat()
        {
            string message = uClient.UserName + " - покидает чат!";
            byte[] data = Encoding.Unicode.GetBytes(message);
            uClient.Client.Send(data, data.Length, Host, remotePort);

            uClient.Client.DropMulticastGroup(groupAdress);
            alive = false;
            uClient.Client.Close();

            logOutButton.IsEnabled = false;
            loginButton.IsEnabled = true;
            sendButton.IsEnabled = false;
        }

        // обработчик события закрытия формы
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ExitChat();
        }

        

    }
}
