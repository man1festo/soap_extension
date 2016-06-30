using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.IO;
using System.Net;
using System.Xml;
using System.Windows.Forms;


    public class TraceExtension : SoapExtension
    {
        Stream wireStream;
        Stream appStream;
        private string soap;
        private string encryptedMsg;
        private string decryptedMsg;

        public string Encrypt(string plainText, string password,
            string salt = "Kosher", string hashAlgorithm = "SHA1",
            int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY",
            int keySize = 256)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes(password, saltValueBytes, hashAlgorithm, passwordIterations);
            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] cipherTextBytes = null;

            using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Clear();
            return Convert.ToBase64String(cipherTextBytes);
        }

        public string Decrypt(string cipherText, string password,
            string salt = "Kosher", string hashAlgorithm = "SHA1",
            int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY",
            int keySize = 256)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes(password, saltValueBytes, hashAlgorithm, passwordIterations);
            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int byteCount = 0;

            using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Clear();
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        }

        // метод в качестве входного параметра
        // получает поток, содержащий передаваемый объект

        public override Stream ChainStream(Stream stream)
        {
            wireStream = stream;
            appStream = new MemoryStream();
            return appStream;
        }

        // ProcessMessage, выполняющий обязательное копирование
        // потоков в двух точках (BeforeDeserialize и AfterSerialize)

        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                // в точке BeforeDeserialize необходимо передать
                // SOAP-запрос из потока сети (wireStream)
                // в поток приложения (appStream)
                case SoapMessageStage.BeforeDeserialize:
                    RecInput(message);
                    //Copy(wireStream, appStream);
                    //appStream.Position = 0;
                    break;
                // в точке AfterSerialize необходимо передать
                // SOAP-ответ из потока приложения в поток сети
                case SoapMessageStage.AfterSerialize:
                    WriteOutput(message);
                    break;
            }

        }
        void Copy(Stream from, Stream to)
        {
            TextReader reader = new StreamReader(from);
            TextWriter writer = new StreamWriter(to);
            writer.WriteLine(reader.ReadToEnd());
            writer.Flush();
        }

        public void RecInput(SoapMessage message)
        {
            Copy(wireStream, appStream);
            appStream.Position = 0;
            XmlDocument documen = new XmlDocument();
            documen.Load(appStream);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(documen.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            XmlNode ResultNode = documen.SelectSingleNode("//soap:Body", nsmgr);
            soap = ResultNode.InnerXml;
            MessageBox.Show("шифр на сервере:" + soap);
            decryptedMsg = Decrypt(soap, "Passpord11", "Password22", "SHA1", 2,
                                                 "16CHARSLONG12345", 256);
            MessageBox.Show("сообщение на сервере:" + decryptedMsg);
            ResultNode.InnerXml = decryptedMsg;
            appStream.SetLength(0);
            appStream.Position = 0;
            documen.Save(appStream);
            appStream.Position = 0;

        }

        public void WriteOutput(SoapMessage message)
        {

            appStream.Position = 0;
            // создадим XML документ из потока
            XmlDocument document = new XmlDocument();
            document.Load(appStream);
            // Для использования XPath нужно определить
            // NamespaceManager
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            // получим ссылку на узел <soap:Body>
            XmlNode ResultNode = document.SelectSingleNode("//soap:Body", nsmgr);
            //сохраним содержимое узла и
            // заменем содержимое узла
            soap = ResultNode.InnerXml;
            MessageBox.Show("Ответ сервера:" + soap);
            encryptedMsg = Encrypt(soap, "Passpord11", "Password22", "SHA1", 2,
                                                 "16CHARSLONG12345", 256);
            MessageBox.Show("Зашифрованный ответ сервера:" + encryptedMsg);
            ResultNode.InnerXml = encryptedMsg;
            // очистим поток и запишем в него новый SOAP-ответ
            appStream.SetLength(0);
            appStream.Position = 0;
            document.Save(appStream);
            // ОБЯЗАТЕЛЬНОЕ ДЕЙСТВИЕ
            // передадим SOAP-ответ из потока приложения (appStream)
            // в поток сети (wireStream)
            appStream.Position = 0;
            Copy(appStream, wireStream);

        }
        // В соотвествии с правилами наследования мы обязаны
        // определить эти методы, однако мы их никак не используем
        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            return null;
        }

        public override object GetInitializer(Type WebServiceType)
        {
            return null;
        }

        public override void Initialize(object initializer)
        {
            return;
        }
    }

