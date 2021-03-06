﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Windows.Markup;
namespace SEN_project_v2
{
    /// <summary>
    /// Interaction logic for Conversation.xaml
    /// </summary>
   
    public partial class Conversation : UserControl
    {
        List<XMLClient.Message> messages;
        ResourceDictionary rd = new ResourceDictionary();
        XMLClient client;
        IPAddress ip;
        public UDP udp;
        public string path;
        public Conversation(IPAddress sender)
        {
            this.Background = MainWindow.brushColor;
            InitializeComponent();
            client = UserList.xml[sender];
            ip = sender;
            path = AppDomain.CurrentDomain.BaseDirectory + ip.ToString().Replace('.', '\\') + "\\" + "Pic.png";
            if(File.Exists(path))
            {
                ProfilePic.Source = new BitmapImage(new Uri(path)).Clone();
            }
        }
        int messIndex = 0;
        int timeIndex=1;
        private void MessagePanel_Loaded(object sender, RoutedEventArgs e)
        {
            Draw();
           
        }
        public static FlowDocument TransformImages(FlowDocument flowDocument,IPAddress ip,int index)
        {
            FlowDocument img_flowDocument = flowDocument;
            Type inlineType;
            InlineUIContainer uic;
            System.Windows.Controls.Image replacementImage;
            int count = 0;
            
            foreach (Block b in flowDocument.Blocks)
            {
                if (b is Paragraph)
                {
                    foreach (Inline i in ((Paragraph)b).Inlines)
                    {

                        inlineType = i.GetType();
                        if (inlineType == typeof(InlineUIContainer))
                        {
                            uic = ((InlineUIContainer)i);


                            if (uic.Child.GetType() == typeof(System.Windows.Controls.Image))
                            {
                                replacementImage = (System.Windows.Controls.Image)uic.Child;

                                string Path = AppDomain.CurrentDomain.BaseDirectory + "\\" + ip.ToString().Replace('.', '\\') + "\\" + index + "." + count + ".jpg";

                                BitmapImage bitmapImage = new BitmapImage(new Uri(Path, UriKind.Absolute));
                                replacementImage.Source = bitmapImage;
                                replacementImage.Height = bitmapImage.Height;
                                replacementImage.Width = bitmapImage.Width;
                                count++;

                            }
                        }
                    }
                }
            }
          
            return img_flowDocument;
        }
               
        public void Redraw()
        {
            MessagePanel.Children.Clear();

            Draw();
            path = AppDomain.CurrentDomain.BaseDirectory + ip.ToString().Replace('.', '\\') + "\\" + "Pic.png";
            if (File.Exists(path))
            {
                ProfilePic.Source = new BitmapImage(new Uri(path)).Clone();
            }
        }
        private void Draw()
        {
            UserList.xml[ip].UnreadMessages = 0;
    
            Dispatcher.BeginInvoke((Action)(() =>
            {
                messages = client.fetchMessages();
                foreach (XMLClient.Message m in messages)
                {
                    if (m.self)
                    {
                        
                        ReceMessage s = new ReceMessage(ip, m.value, m.time.ToString("hh:mm"),client,m.index);
                        s.SetMessage(m);
                        MessagePanel.Children.Add(s);
                    }
                    else
                    {
                        SentMessage s = new SentMessage(ip, m.value, m.time.ToString("hh:mm"), client,m.index);
                        s.SetMessage(m);
                        MessagePanel.Children.Add(s);
                    }
                }
            }));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MemoryStream stream = new MemoryStream();

            using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(stream))
            {
                XamlWriter.Save(MTransformImages(SendBox.Document, ip), writer);
            }

            Byte[] Messeage = stream.GetBuffer().Skip(3).ToArray();

            UserList.xml[ip].addSelfMessage(DateTime.Now, Encoding.ASCII.GetString(Messeage), MainWindow.category);
            udp.SendMessageTo(Encoding.ASCII.GetBytes(UDP.Message).Concat(Messeage).ToArray(), ip, MainWindow.category);
            stream.Close();
            this.Redraw();
        }
        public FlowDocument MTransformImages(FlowDocument flowDocument, IPAddress ip)
        {
            FlowDocument img_flowDocument = flowDocument;
            Type inlineType;
            InlineUIContainer uic;
            System.Windows.Controls.Image replacementImage;
            List<string> files = new List<string>();
            int count = 0;
            int index = UserList.xml[ip].CountMessages;
            foreach (Block b in flowDocument.Blocks)
            {
                if (b is Paragraph)
                {
                    foreach (Inline i in ((Paragraph)b).Inlines)
                    {

                        inlineType = i.GetType();
                        if (inlineType == typeof(InlineUIContainer))
                        {
                            uic = ((InlineUIContainer)i);


                            if (uic.Child.GetType() == typeof(System.Windows.Controls.Image))
                            {
                                replacementImage = (System.Windows.Controls.Image)uic.Child;
                                count=setImages(files, ip, count, index, replacementImage);


                            }
                        }
                    }
                }
                else
                {

                }
            }
            List<IPAddress> ips = new List<IPAddress>();
            ips.Add(ip);
            MainWindow.tcp.SendFiles(files, ips, 1);
            return img_flowDocument;
        }
        private int setImages(List<string> files, IPAddress ip, int count, int index, Image replacementImage)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)replacementImage.Source));
            string Path = AppDomain.CurrentDomain.BaseDirectory + "\\" + ip.ToString().Replace('.', '\\') + "\\" + index + "." + count + ".jpg";
            FileStream stream = new FileStream(Path, FileMode.Create);
            encoder.Save(stream);
            stream.Close();
            BitmapImage bitmapImage = new BitmapImage(new Uri(Path, UriKind.Absolute)).Clone();
            replacementImage.Source = bitmapImage;
            replacementImage.Height = bitmapImage.Height;
            replacementImage.Width = bitmapImage.Width;


            files.Add(Path);
            count++;
            return count;
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            messages = client.fetchMessages();
            
            foreach (XMLClient.Message m in messages)
            {
                UserList.xml[ip].deleteMessage(m);
            }
           
            try
            {
                DirectoryInfo info = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\" + ip.ToString().Replace('.', '\\'));
                foreach (FileInfo file in info.GetFiles())
                {
                    if (file.Name.Split('.').Last().Equals("jpg"))
                    {
                        File.Delete(file.FullName);
                    }
                }
            }
            catch(Exception ex)
            {

            }
            this.Redraw();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.Redraw();
        }

        private void UpdatePic_Click(object sender, RoutedEventArgs e)
        {
            ProfilePic.Source=new BitmapImage(new Uri(
            "pack://application:,,,/Images/user-frame.png", 
                UriKind.Absolute)).Clone() ;
            bool check=true;
            FileStream stream = null;
            try
            {
                if(File.Exists(path))
                stream = File.OpenRead(path);
                check = false;
            }
            catch (IOException)
            {
              
                check= true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            if (!check)
            udp.SendMessageTo(UDP.UpdatePic, ip);


        }

    }

}
