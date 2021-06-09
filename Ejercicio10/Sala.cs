using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ejercicio10
{
    class Sala
    {
        public static List<Socket> clientes = new List<Socket>();
        public static readonly object l = new object();


        public int leePuerto()
        {
            int puerto = 0;

            try
            {
                using (StreamReader sr = new StreamReader("C://temp//puerto.txt"))
                {
                    string linea = sr.ReadLine();

                    if (linea != null)
                    {
                        puerto = Convert.ToInt32(linea);

                        if (puerto >= IPEndPoint.MinPort && puerto <= IPEndPoint.MaxPort)
                        {
                            return puerto;
                        }
                        else
                        {
                            puerto = 10000;
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error al acceder al archivo");
                puerto = 10000;

            }
            return puerto;
        }

        public void envioMensaje(string m, IPEndPoint ie)
        {
            IPEndPoint ieClientes;

            if (m != null)
            {
                lock (l)
                {
                    for (int i = 0; i < clientes.Count; i++)
                    {
                        try
                        {
                            ieClientes = (IPEndPoint)clientes[i].RemoteEndPoint;
                            using (NetworkStream ns = new NetworkStream(clientes[i]))
                            using (StreamWriter sw = new StreamWriter(ns))
                            {
                                if (ie != ieClientes)
                                {
                                    sw.WriteLine(string.Format("{0},{1}: {2}", ieClientes.Address, ieClientes.Port, m));
                                    sw.Flush();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }

        public void iniciaServicioChat()
        {
            int puerto = leePuerto();
            bool flag = true;
            bool correcto = true;


            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, puerto);

            while (correcto)
            {
                try
                {
                    s.Bind(ie);
                    s.Listen(5);
                    correcto = false;
                }
                catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine("Puerto ocupado");
                    correcto = true;
                    if (puerto < IPEndPoint.MaxPort)
                    {
                        puerto++;

                    }
                    else
                    {
                        puerto = 10000;
                        Console.WriteLine("Puerto: " + ie.Port);
                    }

                }
            }

            while (flag)
            {
                Socket sCliente = s.Accept();
                lock (l)
                {
                    clientes.Add(sCliente);
                }
                Thread hilo = new Thread(hiloCliente);
                hilo.Start(sCliente);
            }

        }

        public void hiloCliente(object socket)
        {
            bool conexion = true;
            string msg;
            Socket sCliente = (Socket)socket;
            IPEndPoint ieCliente = (IPEndPoint)sCliente.RemoteEndPoint;
            Console.WriteLine("IP {0}, Puerto {1}", ieCliente.Address, ieCliente.Port);


            using (NetworkStream ns = new NetworkStream(sCliente))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                sw.WriteLine("Bienvenido al server, actualmente hay {0} conectados", clientes.Count);
                sw.Flush();

                while (conexion)
                {
                    try
                    {
                        msg = sr.ReadLine();

                        if (msg != null)
                        {
                            if (msg == "MELARGO")
                            {
                                throw new IOException(); //lanzo  la excepcion para eliminar al cliente
                            }
                            else
                            {
                                envioMensaje(msg, ieCliente);

                            }
                        }


                    }
                    catch (IOException)
                    {

                        conexion = false;
                        Console.WriteLine("Se desconecto en el puerto: " + ieCliente.Port);
                        lock (l)
                        {
                            clientes.Remove(sCliente);
                        }
                        sCliente.Close();
                    }
                }
            }

        }


    }
}
