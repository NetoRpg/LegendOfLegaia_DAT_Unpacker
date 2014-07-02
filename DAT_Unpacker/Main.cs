using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace DAT_Unpacker
{

    public partial class DUP : Form
    {
        public DUP()
        {
            InitializeComponent();
        }

        string _FILEPATH;
        string _FILENAME;

        List<Tree> _FILE;

        private void btnUnpack_Click(object sender, EventArgs e)
        {
            if (_FILEPATH == null) return;

            using (FileStream fs = new FileStream(_FILEPATH, FileMode.Open))
            {
                int fileNum = fs.extractPiece(0, 4, 4).extractInt32() + 1;
                int headerSize = fs.extractPiece(0, 4).extractInt32() * 0x800;
                int[] offsets = new int[fileNum];
                _FILENAME = Path.GetFileNameWithoutExtension(_FILEPATH);
                string basePath = Path.Combine(Path.GetDirectoryName(_FILEPATH), _FILENAME);
                byte[] data;
                _FILE = new List<Tree>();
                int index = -1;

                    // Carregando o header na memória, ganhando velocidade na leitura dos valores.
                    // Menos acessos ao disco, o que é relativamente demorado.
                    MemoryStream header = new MemoryStream(fs.extractPiece(0, headerSize, 0));
                    header.Position = 8;

                    // Fazer a leitura de todos os offsets
                    for (int i = 0; i < fileNum; i++)
                    {
                        offsets[i] = header.extractPiece(0, 4).extractInt32() * 0x800;
                    }


                    Directory.CreateDirectory(basePath);

                    // Extrair e salvar os arquivos.
                    for (int i = 0; i < fileNum - 1; i++)
                    {
                        data = fs.extractPiece(0, offsets[i + 1] - offsets[i]);

                        index++;

                        _FILE.Add(new Tree());
                        _FILE[index].name = String.Format("{0}_{1}.BIN", _FILENAME, i);


                        if (data[3] == 0x01 && data[2] < 0x10)
                        {
                            int timNum = data.extractInt32(4);
                            int[] timOffsets = new int[timNum + 1];

                            Buffer.BlockCopy(data, 0, _FILE[index].header, 0, 4);

                            for (int x = 0; x < timNum; x++)
                            {
                                timOffsets[x] = (data.extractInt32((4 * x) + 8) * 4) + 4;
                            }

                            timOffsets[timOffsets.Length - 1] = data.Length;

                            for (int x = 0; x < timOffsets.Length - 1; x++)
                            {
                                string timPath = Path.Combine(basePath, String.Format("{0}_{1}", _FILENAME, i));
                                Directory.CreateDirectory(timPath);
                                string ext = (data[timOffsets[x]] == 0x10) ? "TIM" : "BIN";


                                _FILE[index].tims.Add(String.Format(@"{0}_{1}\{0}_{1}_{2}.{3}", _FILENAME, i, x, ext));

                                
                                data.Save(Path.Combine(timPath, String.Format("{0}_{1}_{2}.{3}", _FILENAME, i, x, ext)), timOffsets[x], timOffsets[x + 1] - timOffsets[x]);
                            }

                            //continue;
                        }
                        
                        data.Save(Path.Combine(basePath, String.Format("{0}_{1}.BIN", _FILENAME, i)));
                    }

                    saveXML(basePath);
                    MessageBox.Show("Successfully Unpacked!");
                }
            }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "DAT Files|*.DAT";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = _FILEPATH = ofd.FileName;
                }
            }
        }

        private void btnRepack_Click(object sender, EventArgs e)
        {
            string DATinfoPath = null;
            string SavePath = null;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "DATInfo.xml|DATInfo.xml";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    DATinfoPath = ofd.FileName;
                }
            }
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a folder to save the repacked file:";

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SavePath = fbd.SelectedPath;
                }
            }

            if (DATinfoPath == null || SavePath == null) return;

            using (XmlReader reader = XmlReader.Create(DATinfoPath))
            {
                _FILE = new List<Tree>();
                int i = -1;

                while (reader.Read())
                {
                    if (reader.Name.Equals("FILES") && (reader.NodeType == XmlNodeType.Element))
                    {
                        _FILENAME = reader.GetAttribute("Name");
                    }
                    else if (reader.Name.Equals("FILE") && (reader.NodeType == XmlNodeType.Element))
                    {
                        i++;

                        _FILE.Add(new Tree());
                        _FILE[i].name = reader.GetAttribute("Name");
                        _FILE[i].header = Convert.FromBase64String(reader.GetAttribute("Header"));
                    }
                    else if (reader.Name.Equals("TIM") && (reader.NodeType == XmlNodeType.Element))
                    {
                        _FILE[i].tims.Add(reader.GetAttribute("Name"));
                    }

                }
            }

            string basePath = Path.GetDirectoryName(DATinfoPath);
            int headerSize = _FILE.Count * 4;
            while (headerSize % 0x800 != 0)
            {
                headerSize += 1;
            }
            byte[] data;
            using (FileStream fs = File.Create(Path.Combine(SavePath, _FILENAME + ".DAT")))
            {
                fs.Position = headerSize;
                using (MemoryStream ms = new MemoryStream(headerSize))
                {
                    ms.Position = 4;
                    ms.Write(_FILE.Count.int32ToByteArray(), 0, 4);

                    foreach (Tree file in _FILE)
                    {
                        ms.Write(((int)(fs.Position / 0x800)).int32ToByteArray(), 0, 4);

                        if (file.tims.Count > 0)
                        {
                            using (MemoryStream timHeader = new MemoryStream(file.tims.Count * 4 + 8))
                            {
                                timHeader.Write(file.header, 0, 4);
                                timHeader.Write(file.tims.Count.int32ToByteArray(), 0, 4);

                                using (MemoryStream timPack = new MemoryStream())
                                {
                                    timPack.Position = timHeader.Capacity;

                                    foreach (string tim in file.tims)
                                    {
                                        timHeader.Write((((int)timPack.Position - 4) / 4).int32ToByteArray(), 0, 4);
                                        data = File.ReadAllBytes(Path.Combine(basePath, tim));
                                        timPack.Write(data, 0, data.Length);
                                    }

                                    timPack.Position = 0;
                                    timPack.Write(timHeader.ToArray(), 0, (int)timHeader.Length);
                                    fs.Write(timPack.ToArray(), 0, (int)timPack.Length);
                                    while (fs.Position % 0x800 != 0) fs.Position += 1;
                                }
                            }
                            continue;
                        }
                         data = File.ReadAllBytes(Path.Combine(basePath, file.name));
                         fs.Write(data, 0, data.Length);
                         while (fs.Position % 0x800 != 0) fs.Position += 1;
                    }

                    fs.Position = 0;
                    fs.Write(ms.ToArray(), 0, (int)ms.Length);

                }
                
            }
            MessageBox.Show("Successfully Repacked!");


        }


        void saveXML(string path)
        {

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(path, "DATInfo.xml"), settings))
            {
                writer.WriteStartElement("FILES");
                writer.WriteAttributeString("Name", _FILENAME);

                foreach (Tree file in _FILE)
                {       
                    writer.WriteStartElement("FILE");
                    writer.WriteAttributeString("Name", file.name);
                    writer.WriteAttributeString("Header", Convert.ToBase64String(file.header));

                        foreach (string tim in file.tims)
                        {
                            writer.WriteStartElement("TIM");
                            writer.WriteAttributeString("Name", tim);
                            writer.WriteEndElement();
                        }

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
            }
        }



    }
}
