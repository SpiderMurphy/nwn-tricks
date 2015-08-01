using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using NWNMasterServer.libs;

namespace NWNMasterServer
{
    class NWNMasterServer
    {

        // Porta master server di bioware merdosa
        private const int PORT = 5121;
        // Socket server
        private Socket server;
        // Buffer dati
        private byte[] data = new byte[1024];

        // Interfaccia SQL
        private Sql db = new Sql();


        // Costruttore
        public NWNMasterServer()
        {
            
        }


        // Avvia master server
        public void Start()
        {
            try
            {
                // Crea socket
                server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Mette in ascolto su tutte le interfacce
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, PORT);

                // Binda il server
                server.Bind(ipEndPoint);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                // Sender
                EndPoint client = (EndPoint)ipeSender;

                // Riceve dati in modalità asincrona
                server.BeginReceiveFrom(data, 0, data.Length, SocketFlags.None, ref client, new AsyncCallback(OnReceivePacket), client);

            }
            catch (Exception e)
            {
                
            }
        }

        // Arresta master server
        public void Stop()
        {
            try
            {

            }
            catch (Exception e)
            {
            }
        }

        //*********************************************************************************************
        //* Funzione callback ricezione dati
        //*********************************************************************************************

        private void OnReceivePacket(IAsyncResult iar)
        {
            try
            {
                // Socket remoto
                EndPoint remote = (EndPoint)iar.AsyncState;
                // Bytes ricevuti
                int recv = server.EndReceiveFrom(iar, ref remote);

                // Stringa dumping
                String dump = "\n\nPacchetto ricevuto :\n\n" + DumpPacket(data, recv);

                // Handler richieste
                OnPacketReceiveHandler((IPEndPoint)remote, data, recv);

                // Ricomincia 
                data = new byte[1024];
                server.BeginReceiveFrom(data, 0, data.Length, SocketFlags.None, ref remote, new AsyncCallback(OnReceivePacket), remote);
            }
            catch (Exception e)
            {
                
            }
        }


        //*********************************************************************************************
        //* Async send
        //*********************************************************************************************

        private void SendAsync(IAsyncResult iar)
        {
            // Socket remoto
            EndPoint remote = (EndPoint)iar.AsyncState;

            int sent = server.EndSendTo(iar);
        }



        //*********************************************************************************************
        //* Enum messaggi pacchetti
        //*********************************************************************************************

        private enum PacketCmd : uint
        {
            // Client di gioco o server di gioco a master server

            // Autenticazione
            auth_req_Account = 0x41504d42, // Autorizzazione account, BMPA
            auth_req_CdKey = 0x55414d42,   // Autorizzazione cdkey, BMAU

            // Generiche
            gen_req_Motd = 0x414d4d42,     // Messaggio del giorno, BMMA
            gen_req_vers = 0x41524d42,     // Richiesta versione, BMRA
            gen_req_stat = 0x54534d42,     // Richiesta status Master Server, BMST

            // Richieste server
            srv_req_name = 0x53454e42,     // Richiesta nome server, BNES

            // Master server --> game clients, game servers

            // Risposte autenticazione
            auth_res_Account = 0x52504d42, // Risposta autorizzazione account, BMPR
            auth_res_CdKey = 0x52414d42,   // Risposta autorizzazione cdkey, BMAR

            // Generiche
            gen_res_Motd = 0x424d4d42,     // Risposta messaggio del giorno, BMMR
            gen_res_vers = 0x42524d42,     // Risposta versione, BMRB
            gen_res_stat = 0x52534d42,     // Risposta status master server, BMSR

            // Risposte server
            srv_res_name = 0x52454e42      // Risposta nome server, BNER
        }


        //*********************************************************************************************
        //* Strutture dati richieste
        //*********************************************************************************************


        //********************************************************************************************
        //* Handler metodi
        //********************************************************************************************

        private void OnPacketReceiveHandler(IPEndPoint sender, byte[] packet, int len)
        {
            // 
            try
            {
                // Tipo di pacchetto
                uint reqtype = BitConverter.ToUInt32(packet, 0);
                // buffer
                byte[] buffer = new byte[len];

                // Copia buffer
                Buffer.BlockCopy(packet, 4, buffer, 0, len - 4);

                if (reqtype == (uint)PacketCmd.auth_req_Account)
                    OnCommunityAuthorizationRequest(sender, buffer);
                else if (reqtype == (uint)PacketCmd.auth_req_CdKey)
                    OnCdKeyAuthorizationRequest(sender, buffer);
                else if (reqtype == (uint)PacketCmd.gen_req_Motd)
                    SendMotdResponse(sender, "Master Server 1");
                else if (reqtype == (uint)PacketCmd.gen_req_vers)
                    SendVersionResponse(sender, "8109");
                else if (reqtype == (uint)PacketCmd.gen_req_stat)
                    SendMstStatusFlag(sender, 0x0000);
                else if (reqtype == (uint)PacketCmd.srv_req_name)
                    OnServerNameRequest(sender, buffer);

            }
            catch (Exception e)
            {
            }
        }


        //*********************************************************************************************
        //* Metodi eleborazione richieste
        //*********************************************************************************************

        // Richiesta autorizzazione cdkey
        private void OnCdKeyAuthorizationRequest(IPEndPoint sender, byte[] buffer)
        {
            try
            {
                UInt16 port;
                UInt16 keycount;
                UInt32 ip;
                UInt16 playerport;
                byte[] salt;
                UInt16 len;
                List<CDKeyInfo> keys;
                String playername;

                // Interazzi
                port = BitConverter.ToUInt16(buffer, 0);
                keycount = BitConverter.ToUInt16(buffer, 2);
                ip = BitConverter.ToUInt32(buffer, 4);
                playerport = BitConverter.ToUInt16(buffer, 8);
                len = BitConverter.ToUInt16(buffer, 10);

                // Imposta salt
                salt = new byte[len];

                // Bytazzi bufferazzi
                Buffer.BlockCopy(buffer, 12, salt, 0, len);

                // Contatore entries
                UInt16 count = BitConverter.ToUInt16(buffer, 12 + len);
                // Imposta lista
                keys = new List<CDKeyInfo>();
                // Offset
                int offset = 12 + len + 2;
                int index = count;

                while (index-- > 0)
                {
                    CDKeyInfo key = new CDKeyInfo();
                    // Lunghezza chiave pubblica
                    UInt16 pklen = BitConverter.ToUInt16(buffer, offset);
                    key.PublicCDKey = Encoding.UTF8.GetString(buffer, offset + 2, pklen);
                    offset += pklen + 2;
                    // Lunghezza hash
                    UInt16 hlen = BitConverter.ToUInt16(buffer, offset);
                    key.CDKeyHash = new byte[hlen];
                    Buffer.BlockCopy(buffer, offset + 2, key.CDKeyHash, 0, hlen);
                    offset += 2 + hlen;

                    // Aggiunge chiave alla lista
                    keys.Add(key);
                }

                // Legge lunghezza username
                UInt16 plen = BitConverter.ToUInt16(buffer, offset);
                playername = Encoding.UTF8.GetString(buffer, offset + 2, plen);

                SendCdKeyAuthorizationResponse(sender, keys, "");
            }
            catch (Exception e)
            {
            }
        }

        // Richiesta autorizzazione account
        private void OnCommunityAuthorizationRequest(IPEndPoint sender, byte[] buffer)
        {
            try
            {
                UInt16 port;     // Porta
                UInt16 len;      // Lunghezza buffer
                byte[] salt;     // Salt
                String username; // Username
                byte[] hash;     // Hash password
                UInt16 lang;     // Lingua client
                byte os;         // Sistema operativo cliente
                byte type;       // DM or player

                port = BitConverter.ToUInt16(buffer, 0);
                len = BitConverter.ToUInt16(buffer, 2);

                byte[] md5buff = new byte[64];

                // Imposta dimensione buffer
                salt = new byte[len];
                hash = new byte[len];

                // Copia buffer salt
                Buffer.BlockCopy(buffer, 4, salt, 0, salt.Length);
                UInt16 ulen = BitConverter.ToUInt16(buffer, 4 + len);
                // Ottiene stringa
                username = Encoding.UTF8.GetString(buffer, (4 + len + 2), ulen);
                // Copia buffer hash
                Buffer.BlockCopy(buffer, 5 + len + 1 + username.Length + 2, hash, 0, len);
                // Lingua
                lang = BitConverter.ToUInt16(buffer, buffer.Length - 4);



                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

                //String chall = GetServerChallenge(salt);
                // Password hash
                UInt16 auth = 1;

                // Errore SQL
                String sqlerror = "";

                // Se l'account esiste procede con la verifica della password, in caso contrario
                // permette di loggare sul server in modo tale da registrare l'account e la password
                // sul db di gioco.
                //
                // NB : Richiede che i gioco sia interfacciato con un database SQL tramite nwnx.
                if (db.GetAccountExists(username, out sqlerror))
                {
                    // Esegue autenticazione
                    if (db.DoAuthUsername(username, Encoding.UTF8.GetString(hash), salt, out sqlerror))
                        auth = 0;
                }


                SendCommunityAuthorizationResponse(sender, username, auth);
            }
            catch (Exception e)
            {
               
            }
        }


        // Richiesta heartbeat da parte del client
        private void OnServerNameRequest(IPEndPoint sender, byte[] buffer)
        {
            try
            {
                UInt16 port = BitConverter.ToUInt16(buffer, 0);
                //byte pad;

            }
            catch (Exception e)
            {
            }
        }

        //*********************************************************************************************
        //* Metodi invio risposte
        //*********************************************************************************************

        // Invia risposta autenticazione CDkey
        private void SendCdKeyAuthorizationResponse(IPEndPoint sender, List<CDKeyInfo> cdkeys, String username)
        {
            try
            {
                // Risposte
                byte[] packet = new Byte[4 + 2 + (cdkeys.ElementAt(0).PublicCDKey.Length * cdkeys.Count) + 6 * cdkeys.Count];

                // Imposta parametri
                Buffer.BlockCopy(BitConverter.GetBytes((uint)PacketCmd.auth_res_CdKey), 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cdkeys.Count), 0, packet, 4, 2);

                // Coia cd key
                int offset = 4 + 2;

                for (int i = 0; i < cdkeys.Count; i++)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cdkeys.ElementAt(i).PublicCDKey.Length), 0, packet, offset, 2);
                    offset += 2;
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(cdkeys.ElementAt(i).PublicCDKey), 0, packet, offset, cdkeys.ElementAt(i).PublicCDKey.Length);
                    offset += cdkeys.ElementAt(i).PublicCDKey.Length;
                    Buffer.BlockCopy(BitConverter.GetBytes((UInt16)0), 0, packet, offset, 2);
                    offset += 2;
                    Buffer.BlockCopy(BitConverter.GetBytes((UInt16)i), 0, packet, offset, 2);
                    offset += 2;
                }

                String dump = "\n\nPacchetto inviato :\n\n Porta:" + sender.Port.ToString() + "  -> " + DumpPacket(packet, packet.Length);

                server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, (EndPoint)sender, new AsyncCallback(SendAsync), sender);
            }
            catch (Exception e)
            {
            }
        }

        // Community Authorization response
        private void SendCommunityAuthorizationResponse(IPEndPoint sender, String username, ushort status)
        {
            try
            {
                // Compone pacchetto
                byte[] packet = new byte[username.Length + 6 + 2];

                Buffer.BlockCopy(BitConverter.GetBytes((uint)PacketCmd.auth_res_Account), 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)username.Length), 0, packet, 4, 2);
                Buffer.BlockCopy(Encoding.UTF8.GetBytes(username), 0, packet, 6, username.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(status), 0, packet, 6 + username.Length, 2);


                // Stringa dumping
                String dump = "\n\nPacchetto inviato :\n\n Porta:" + sender.Port.ToString() + "  -> " + DumpPacket(packet, packet.Length);

                // Invia risposta
                server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, (EndPoint)sender, new AsyncCallback(SendAsync), sender);
            }
            catch (Exception e)
            {
                
            }
        }

        // Messaggio del giorno
        private void SendMotdResponse(IPEndPoint sender, String message)
        {
            try
            {
                // Cmpone pacchetto
                byte[] packet = new byte[message.Length + 6];

                Buffer.BlockCopy(BitConverter.GetBytes((uint)PacketCmd.gen_res_Motd), 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)message.Length), 0, packet, 4, 2);
                Buffer.BlockCopy(Encoding.UTF8.GetBytes(message), 0, packet, 6, message.Length);

                // Invia risposta
                server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, (EndPoint)sender, new AsyncCallback(SendAsync), sender);
            }
            catch (Exception e)
            {

            }
        }

        // Invia versione software in uso
        private void SendVersionResponse(IPEndPoint sender, String version)
        {
            try
            {
                // Compone pacchetto
                byte[] packet = new byte[version.Length + 6];

                Buffer.BlockCopy(BitConverter.GetBytes((uint)PacketCmd.gen_res_vers), 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)version.Length), 0, packet, 4, 2);
                Buffer.BlockCopy(Encoding.UTF8.GetBytes(version), 0, packet, 6, version.Length);

                // Invia risposta
                server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, (EndPoint)sender, new AsyncCallback(SendAsync), sender);
            }
            catch (Exception e)
            {
                
            }
        }

        private void SendMstStatusFlag(IPEndPoint sender, ushort status)
        {
            try
            {
                byte[] packet = new byte[6];

                Buffer.BlockCopy(BitConverter.GetBytes((uint)PacketCmd.gen_res_stat), 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(status), 0, packet, 4, 2);

                // Invia risposta
                server.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, (EndPoint)sender, new AsyncCallback(SendAsync), sender);
            }
            catch (Exception e)
            {
            }
        }

        //*********************************************************************************************
        //* Funzione elaborazione stringhe da buffer
        //*********************************************************************************************

        // Dump pacchetti
        private String DumpPacket(byte[] packet, int len)
        {
            // Dump
            String dump = "";

            try
            {
                for (int i = 0; i < len; i++)
                {
                    if (packet[i] == 0)
                        dump += "%";
                    else
                        dump += Encoding.ASCII.GetString(packet, i, 1);
                }
            }
            catch (Exception e)
            {
            }

            // Exit
            return dump;
        }


        // Ottiene hash
        String GetHexString(byte[] buffer)
        {
            String hex = "";

            for (int i = 0; i < buffer.Length; i++)
                hex += buffer[i].ToString("x2");

            return hex;
        }


        // Classe lista cdkeys
        public struct CDKeyInfo
        {
            public string PublicCDKey;
            public byte[] CDKeyHash;
            public UInt16 AuthStatus;
            public UInt16 Product;
        }

        //****************************************************************************************************
        //* Classe client per gestione connessioni
        //****************************************************************************************************

        // Account utente
        private class Account
        {
            public String username;
            public String password;
            public Boolean isdm;
        }
    }
}
