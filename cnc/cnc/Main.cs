using System;
using System.Reflection;
using System.IO.Ports;

namespace cnc
{

	public delegate void CNCEventHandler(object sender, CNCEventArgs e);

	public class CNCEventArgs : EventArgs
	{
		public bool avanzar;
        public string axis;
		public CNCEventArgs (bool avanzar, string axis)
		{
			this.avanzar = avanzar;
            this.axis = axis;
		}

	}

	public class Main
	{
        public AxisXY axisXY;
        public AxisZ axisZ;
        public string unit;
        public string distanceMode;
        MethodInfo[] metodos;
        Taladro taladro;
        public static SerialPort port;
        public static byte data;
        bool alredySetup;
        int xyMaxFeedRate, zMaxFeedRate;
        int axisXStepsPercm, axisYStepsPercm, axisZStepsPercm;

		public event CNCEventHandler makeStep;

        public Main (string portName, int baudRate, int axisXStepsPercm, int axisYStepsPercm, int axisZStepsPercm, int xyMaxFeedRate, int zMaxFeedRate)
		{
            Type miTipo = typeof(Main);
            port = new SerialPort(portName, baudRate);
            Console.WriteLine(portName);
            taladro = new Taladro();
            metodos = miTipo.GetMethods();
            alredySetup = false;
            this.xyMaxFeedRate = xyMaxFeedRate;
            this.zMaxFeedRate = zMaxFeedRate;
            this.axisXStepsPercm = axisXStepsPercm;
            this.axisYStepsPercm = axisYStepsPercm;
            this.axisZStepsPercm = axisZStepsPercm;
		}

        void HandlemakeStep (object sender, CNCEventArgs e)
        {
			Console.WriteLine("main noto step");
			makeStep(sender,e);
        }

        public static void Refresh()
        {
            Console.WriteLine("Enviando: "+data.ToString());
            port.Write(new byte[]{data},0,1);
                
        }

        public void Handle(string comando)
        {
            string[] parametros;
            string[] comandos;
            port.Open();
            try
            {
                comando = comando.Trim();
                comandos = comando.Split(' ');
                Console.WriteLine("Cantidad de comandos: "+comandos.Length);

                if (comandos.Length > 1)
                {
                    parametros = new string[comandos.Length - 1];
                    for (int i = 0; i < comandos.Length-1; i++)
                    {
                        parametros[i] = comandos[i + 1].Trim().Replace('.',',');
                        Console.WriteLine("Asignando "+ parametros[i] +" a parametro: "+i);
                    }
                }
                else
                    parametros = null;

                foreach (MethodInfo metodo in metodos)
                    if (metodo.Name == comandos[0])
                    {
                        try
                        {
                            if (metodo.GetParameters().Length == parametros.Length)
                                metodo.Invoke(this, parametros);
                        }
                        catch
                        {
                            metodo.Invoke(this, parametros);
                        }
                    }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            port.Close();  

        }

        void Setup()
        {
            alredySetup = true;
            axisXY = new AxisXY(distanceMode, unit, axisXStepsPercm, axisYStepsPercm);
            axisZ = new AxisZ(distanceMode, unit, axisZStepsPercm);
            axisXY.makeStep += new CNCEventHandler(HandlemakeStep);
            axisZ.makeStep += new CNCEventHandler(HandlemakeStep);
        }

		public void G21()
		{
			unit = "millimeters";
		}
        public void G20()
		{
            unit = "inches";
		}
        public void G90()
		{
            distanceMode = "absolute";
            if (!alredySetup)
                Setup();
		}
        public void G91()
		{
            distanceMode = "incremental";
            if (!alredySetup)
                Setup();
		}

        //funca
        public void G00(string xString, string yString)
		{            
            float x = float.Parse(xString.Remove(0, 1));
            float y = float.Parse(yString.Remove(0, 1));

            Console.WriteLine("Float x: "+x);

			axisXY.Move(x,y,xyMaxFeedRate);
		}
        //no funca
        public void G00(string zString)
		{
            float z = float.Parse(zString.Remove(0, 1));

            Console.WriteLine("Float z: " + z);

            axisZ.Move(z,zMaxFeedRate);
		}
        public void M03()
		{
            taladro.StartClockWise();      
		}
        public void M04()
		{
            taladro.StartCounterClockWise();
        }
        public void M05()
		{
            taladro.Stop();
        }
        public void G04(string seconds)
		{
            seconds = seconds.Remove(0, 1);
            double segundos = double.Parse(seconds);
            Console.WriteLine("Segundos:"+segundos);
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(segundos));
        }

        public void G01(string xz, string yf)
        {
            if (xz[0] == 'X')
            {
                Console.WriteLine("Float x:" + xz);
                Console.WriteLine("Float y:" + yf);
                axisXY.Move(float.Parse(xz.Remove(0, 1)), float.Parse(yf.Remove(0, 1)));
            }
            else
            {
                Console.WriteLine("Float z:" + xz);
                Console.WriteLine("Float f:" + yf);
                axisZ.Move(float.Parse(xz.Remove(0, 1)), float.Parse(yf.Remove(0, 1)));
            }

        }

        //funca
        public void G01(string xString, string yString, string fString)
		{
            float x = float.Parse(xString.Remove(0, 1));
            float y = float.Parse(yString.Remove(0, 1));
            float f = float.Parse(fString.Remove(0, 1));

            Console.WriteLine("Float x: " + x);

            axisXY.Move(x, y, f);
        }

        public void G82(string x, string y, string z, string f, string r, string p)
        { 
            G01(z,f);
            G01(x,y);
        }

        public void G82(string x, string y)
        {
            G01(x,y);
        }

        public void M02()
		{
            
        }



	}
}

