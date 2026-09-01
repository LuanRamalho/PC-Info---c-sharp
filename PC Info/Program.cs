using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PC_Info
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TabControl tabControl = null!;

        private TextBox txtMotherboard = null!;
        private TextBox txtCpu = null!;
        private TextBox txtRam = null!;
        private TextBox txtGpu = null!;
        private TextBox txtStorage = null!;
        private TextBox txtOs = null!;

        private Label lblStatus = null!;
        private Button btnRefresh = null!;

        public MainForm()
        {
            InitializeInterface();
            _ = LoadInformationAsync();
        }

        // ==========================================================
        // INTERFACE
        // ==========================================================

        private void InitializeInterface()
        {
            Text = "Informações completas do PC";
            StartPosition = FormStartPosition.CenterScreen;

            Width = 1150;
            Height = 760;

            MinimumSize = new Size(950, 600);

            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 10F);

            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(35, 40, 48)
            };

            Label title = new Label
            {
                Text = "INFORMAÇÕES COMPLETAS DO PC",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 16)
            };

            btnRefresh = new Button
            {
                Text = "Atualizar",
                Width = 115,
                Height = 38,
                Location = new Point(995, 15),

                Anchor = AnchorStyles.Top | AnchorStyles.Right,

                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,

                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnRefresh.FlatAppearance.BorderSize = 0;

            btnRefresh.Click += async (_, _) =>
            {
                await LoadInformationAsync();
            };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(btnRefresh);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                Appearance = TabAppearance.Normal,
                Padding = new Point(18, 8)
            };

            txtMotherboard = CreateInfoTextBox();
            txtCpu = CreateInfoTextBox();
            txtRam = CreateInfoTextBox();
            txtGpu = CreateInfoTextBox();
            txtStorage = CreateInfoTextBox();
            txtOs = CreateInfoTextBox();

            AddTab("Placa-mãe", txtMotherboard);
            AddTab("Processador", txtCpu);
            AddTab("Memória RAM", txtRam);
            AddTab("GPU", txtGpu);
            AddTab("HD/SSD", txtStorage);
            AddTab("Sistema Operacional", txtOs);

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = Color.FromArgb(230, 233, 238)
            };

            lblStatus = new Label
            {
                Text = "Inicializando...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            bottomPanel.Controls.Add(lblStatus);

            Controls.Add(tabControl);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private TextBox CreateInfoTextBox()
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,

                ScrollBars = ScrollBars.Both,
                WordWrap = false,

                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 30, 30),

                BorderStyle = BorderStyle.None,

                Font = new Font("Consolas", 11F),

                Padding = new Padding(15),

                TabStop = false
            };
        }

        private void AddTab(string title, TextBox textBox)
        {
            TabPage tab = new TabPage(title)
            {
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            tab.Controls.Add(textBox);
            tabControl.TabPages.Add(tab);
        }

        // ==========================================================
        // CARREGAMENTO
        // ==========================================================

        private async Task LoadInformationAsync()
        {
            btnRefresh.Enabled = false;

            lblStatus.Text = "Lendo informações do computador...";

            txtMotherboard.Text = "Consultando placa-mãe...";
            txtCpu.Text = "Consultando processador...";
            txtRam.Text = "Consultando memória RAM...";
            txtGpu.Text = "Consultando GPU...";
            txtStorage.Text = "Consultando HD/SSD...";
            txtOs.Text = "Consultando sistema operacional...";

            try
            {
                PcInformation info =
                    await Task.Run(ReadAllInformation);

                txtMotherboard.Text = info.Motherboard;
                txtCpu.Text = info.Cpu;
                txtRam.Text = info.Ram;
                txtGpu.Text = info.Gpu;
                txtStorage.Text = info.Storage;
                txtOs.Text = info.OperatingSystem;

                lblStatus.Text =
                    $"Última atualização: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
            catch (Exception ex)
            {
                string error =
                    "Não foi possível concluir a leitura das informações.\r\n\r\n" +
                    "Erro:\r\n" +
                    ex.Message;

                txtMotherboard.Text = error;
                txtCpu.Text = error;
                txtRam.Text = error;
                txtGpu.Text = error;
                txtStorage.Text = error;
                txtOs.Text = error;

                lblStatus.Text =
                    "Erro ao consultar o computador.";
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private PcInformation ReadAllInformation()
        {
            return new PcInformation
            {
                Motherboard = ReadMotherboard(),
                Cpu = ReadCpu(),
                Ram = ReadRam(),
                Gpu = ReadGpu(),
                Storage = ReadStorage(),
                OperatingSystem = ReadOperatingSystem()
            };
        }

        // ==========================================================
        // PLACA-MÃE
        // ==========================================================

        private string ReadMotherboard()
        {
            try
            {
                string manufacturer = "N/D";
                string product = "N/D";
                string model = "N/D";
                string serial = "N/D";
                string version = "N/D";
                string status = "N/D";

                bool boardFound = false;

                // --------------------------------------------------
                // Win32_BaseBoard
                // --------------------------------------------------

                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject board in searcher.Get())
                    {
                        manufacturer =
                            GetString(board, "Manufacturer");

                        product =
                            GetString(board, "Product");

                        model =
                            GetString(board, "Model");

                        serial =
                            GetString(board, "SerialNumber");

                        version =
                            GetString(board, "Version");

                        status =
                            GetString(board, "Status");

                        boardFound = true;

                        break;
                    }
                }

                if (!boardFound)
                    return "Informações da placa-mãe não encontradas.";

                // --------------------------------------------------
                // Win32_ComputerSystem
                // --------------------------------------------------

                string sysManufacturer = "N/D";
                string sysModel = "N/D";
                string sysFamily = "N/D";
                string sysSku = "N/D";

                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject system in searcher.Get())
                    {
                        sysManufacturer =
                            GetString(system, "Manufacturer");

                        sysModel =
                            GetString(system, "Model");

                        sysFamily =
                            GetString(system, "SystemFamily");

                        sysSku =
                            GetString(system, "SystemSKUNumber");

                        break;
                    }
                }

                // --------------------------------------------------
                // Win32_ComputerSystemProduct
                // --------------------------------------------------

                string productVendor = "N/D";
                string productName = "N/D";
                string productVersion = "N/D";
                string productIdentifyingNumber = "N/D";

                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject sysProduct in searcher.Get())
                    {
                        productVendor =
                            GetString(sysProduct, "Vendor");

                        productName =
                            GetString(sysProduct, "Name");

                        productVersion =
                            GetString(sysProduct, "Version");

                        productIdentifyingNumber =
                            GetString(
                                sysProduct,
                                "IdentifyingNumber");

                        break;
                    }
                }

                // --------------------------------------------------
                // BIOS
                // --------------------------------------------------

                string biosManufacturer = "N/D";
                string biosVersion = "N/D";
                string biosReleaseDate = "N/D";
                string biosSerial = "N/D";

                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_BIOS"))
                {
                    foreach (ManagementObject bios in searcher.Get())
                    {
                        biosManufacturer =
                            GetString(
                                bios,
                                "Manufacturer");

                        biosVersion =
                            GetString(
                                bios,
                                "SMBIOSBIOSVersion");

                        biosReleaseDate =
                            FormatWmiDate(
                                GetString(
                                    bios,
                                    "ReleaseDate"));

                        biosSerial =
                            GetString(
                                bios,
                                "SerialNumber");

                        break;
                    }
                }

                // --------------------------------------------------
                // Identificação mais útil do equipamento
                // --------------------------------------------------

                string notebookModel =
                    FirstAvailable(
                        sysModel,
                        productName,
                        productVersion,
                        sysFamily);

                string boardCommercialId =
                    FirstAvailable(
                        model,
                        product,
                        sysModel,
                        productName);

                return
                    "══════════════════════════════════════════════════════\r\n" +
                    " PLACA-MÃE\r\n" +
                    "══════════════════════════════════════════════════════\r\n\r\n" +

                    $"Fabricante................: {manufacturer}\r\n" +
                    $"Produto / código.........: {product}\r\n" +
                    $"Modelo da placa...........: {model}\r\n" +
                    $"Número de série...........: {serial}\r\n" +
                    $"Versão....................: {version}\r\n" +
                    $"Status....................: {status}\r\n\r\n" +

                    "══════════════════════════════════════════════════════\r\n" +
                    " IDENTIFICAÇÃO DO NOTEBOOK\r\n" +
                    "══════════════════════════════════════════════════════\r\n\r\n" +

                    $"Fabricante do sistema.....: {sysManufacturer}\r\n" +
                    $"Modelo do sistema.........: {sysModel}\r\n" +
                    $"Família do sistema........: {sysFamily}\r\n" +
                    $"SKU do sistema............: {sysSku}\r\n" +
                    $"Fabricante do produto.....: {productVendor}\r\n" +
                    $"Nome do produto...........: {productName}\r\n" +
                    $"Versão do produto.........: {productVersion}\r\n" +
                    $"Identificador do produto..: {productIdentifyingNumber}\r\n\r\n" +

                    $"Identificação principal...: {notebookModel}\r\n" +
                    $"Identificador da placa....: {boardCommercialId}\r\n\r\n" +

                    "══════════════════════════════════════════════════════\r\n" +
                    " BIOS / SMBIOS\r\n" +
                    "══════════════════════════════════════════════════════\r\n\r\n" +

                    $"Fabricante da BIOS........: {biosManufacturer}\r\n" +
                    $"Versão da BIOS............: {biosVersion}\r\n" +
                    $"Data da BIOS..............: {biosReleaseDate}\r\n" +
                    $"Serial da BIOS............: {biosSerial}\r\n\r\n" +

                    "Observação:\r\n" +
                    "O campo \"Modelo da placa\" pertence à identificação\r\n" +
                    "da placa-mãe em Win32_BaseBoard. Alguns fabricantes\r\n" +
                    "de notebooks não preenchem esse campo, por isso ele\r\n" +
                    "pode aparecer como N/D.\r\n\r\n" +

                    "Neste caso, o código do produto da placa e as\r\n" +
                    "informações de sistema/SMBIOS são utilizados para\r\n" +
                    "identificar corretamente o equipamento.\r\n";
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar a placa-mãe:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // PROCESSADOR
        // ==========================================================

        private string ReadCpu()
        {
            try
            {
                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_Processor");

                ManagementObjectCollection results =
                    searcher.Get();

                CpuIdFrequencyInfo cpuid =
                    GetCpuIdFrequencyInfo();

                List<string> processors =
                    new List<string>();

                int index = 1;

                foreach (ManagementObject cpu in results)
                {
                    string name =
                        GetString(cpu, "Name");

                    string manufacturer =
                        GetString(cpu, "Manufacturer");

                    string friendlyManufacturer =
                        GetFriendlyManufacturer(
                            manufacturer);

                    string description =
                        GetString(cpu, "Description");

                    string deviceId =
                        GetString(cpu, "DeviceID");

                    string socket =
                        GetString(cpu, "SocketDesignation");

                    string processorId =
                        GetString(cpu, "ProcessorId");

                    string family =
                        GetString(cpu, "Family");

                    string revision =
                        GetString(cpu, "Revision");

                    string architecture =
                        GetArchitecture(
                            GetUInt16(cpu, "Architecture"));

                    uint cores =
                        GetUInt32(
                            cpu,
                            "NumberOfCores");

                    uint logicalProcessors =
                        GetUInt32(
                            cpu,
                            "NumberOfLogicalProcessors");

                    uint currentClock =
                        GetUInt32(
                            cpu,
                            "CurrentClockSpeed");

                    uint maxClockWmi =
                        GetUInt32(
                            cpu,
                            "MaxClockSpeed");

                    uint l2 =
                        GetUInt32(
                            cpu,
                            "L2CacheSize");

                    uint l3 =
                        GetUInt32(
                            cpu,
                            "L3CacheSize");

                    uint threadCount =
                        GetUInt32(
                            cpu,
                            "ThreadCount");

                    // --------------------------------------------------
                    // Frequências
                    // --------------------------------------------------

                    uint baseMHz = cpuid.BaseMHz;
                    uint turboMHz = cpuid.MaxTurboMHz;

                    string frequencySource =
                        "CPUID";

                    // --------------------------------------------------
                    // FALLBACK OFICIAL PARA INTEL CORE i3-N300
                    // --------------------------------------------------

                    if (IsIntelCoreI3N300(name))
                    {
                        // Intel Core i3-N300:
                        // CPU HFM = 800 MHz
                        // Max Burst = 3800 MHz

                        if (baseMHz == 0)
                            baseMHz = 800;

                        if (turboMHz == 0)
                            turboMHz = 3800;

                        frequencySource =
                            cpuid.Supported
                                ? "CPUID + especificação do Intel Core i3-N300"
                                : "especificação oficial do Intel Core i3-N300";
                    }

                    string currentClockLine =
                        currentClock > 0
                            ? FormatMHz(currentClock)
                            : "N/D";

                    string wmiMaxClockLine =
                        maxClockWmi > 0
                            ? FormatMHz(maxClockWmi)
                            : "N/D";

                    string baseClockLine =
                        baseMHz > 0
                            ? FormatMHz(baseMHz)
                            : "N/D";

                    string turboClockLine =
                        turboMHz > 0
                            ? FormatMHz(turboMHz)
                            : "N/D";

                    string cpuidStatus;

                    if (cpuid.Supported)
                    {
                        cpuidStatus =
                            $"Disponível — CPUID 0x16\r\n" +
                            $"Base lida...............: {FormatMHz(cpuid.BaseMHz)}\r\n" +
                            $"Turbo lido..............: {FormatMHz(cpuid.MaxTurboMHz)}\r\n" +
                            $"Barramento..............: {FormatMHz(cpuid.BusMHz)}";
                    }
                    else
                    {
                        cpuidStatus =
                            "Não disponível neste processador/ambiente.";
                    }

                    processors.Add(
                        "══════════════════════════════════════════════════════\r\n" +
                        $" PROCESSADOR {index}\r\n" +
                        "══════════════════════════════════════════════════════\r\n\r\n" +

                        $"Nome.....................: {name}\r\n" +
                        $"Fabricante................: {friendlyManufacturer}\r\n" +
                        $"Identificação original....: {manufacturer}\r\n" +
                        $"Descrição.................: {description}\r\n" +
                        $"ID do dispositivo........: {deviceId}\r\n" +
                        $"Socket....................: {socket}\r\n" +
                        $"Arquitetura...............: {architecture}\r\n" +
                        $"Núcleos físicos...........: {cores}\r\n" +
                        $"Processadores lógicos.....: {logicalProcessors}\r\n" +
                        $"Threads...................: {threadCount}\r\n\r\n" +

                        "FREQUÊNCIAS\r\n" +
                        "──────────────────────────────────────────────────────\r\n" +
                        $"Clock atual (WMI)........: {currentClockLine}\r\n" +
                        $"Máximo informado pelo WMI: {wmiMaxClockLine}\r\n" +
                        $"Frequência base.........: {baseClockLine}\r\n" +
                        $"Turbo / Burst máximo....: {turboClockLine}\r\n" +
                        $"Fonte da frequência.....: {frequencySource}\r\n\r\n" +

                        "CACHE\r\n" +
                        "──────────────────────────────────────────────────────\r\n" +
                        $"Cache L2.................: {FormatKB(l2)}\r\n" +
                        $"Cache L3.................: {FormatKB(l3)}\r\n\r\n" +

                        "IDENTIFICAÇÃO\r\n" +
                        "──────────────────────────────────────────────────────\r\n" +
                        $"Família..................: {family}\r\n" +
                        $"Revisão..................: {revision}\r\n" +
                        $"Processor ID.............: {processorId}\r\n\r\n" +

                        "CPUID\r\n" +
                        "──────────────────────────────────────────────────────\r\n" +
                        cpuidStatus +
                        "\r\n"
                    );

                    index++;
                }

                string result =
                    processors.Count > 0
                        ? string.Join(
                            "\r\n\r\n",
                            processors)
                        : "Processador não encontrado.";

                result +=
                    "\r\n\r\n══════════════════════════════════════════════════════\r\n" +
                    " IMPORTANTE SOBRE AS FREQUÊNCIAS\r\n" +
                    "══════════════════════════════════════════════════════\r\n\r\n" +
                    "O campo \"Clock atual\" mostra a frequência do processador\r\n" +
                    "no momento da leitura. Ela varia constantemente conforme\r\n" +
                    "carga, temperatura e gerenciamento de energia.\r\n\r\n" +

                    "O campo \"Máximo informado pelo WMI\" é o valor retornado\r\n" +
                    "pelo firmware/SMBIOS através do Win32_Processor. Em alguns\r\n" +
                    "notebooks esse valor não representa corretamente o Turbo\r\n" +
                    "máximo do processador.\r\n\r\n" +

                    "Por isso o programa separa esse valor da frequência base\r\n" +
                    "e do Turbo/Burst máximo.\r\n";

                return result;
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar o processador:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // IDENTIFICAÇÃO DO i3-N300
        // ==========================================================

        private static bool IsIntelCoreI3N300(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string text =
                name.ToUpperInvariant();

            return
                text.Contains("I3-N300") ||
                text.Contains("I3 N300") ||
                text.Contains("CORE(TM) I3-N300") ||
                text.Contains("CORE I3-N300");
        }

        // ==========================================================
        // FABRICANTE DO PROCESSADOR
        // ==========================================================

        private static string GetFriendlyManufacturer(
            string cpuidVendor)
        {
            if (string.IsNullOrWhiteSpace(cpuidVendor) ||
                cpuidVendor == "N/D")
            {
                return "N/D";
            }

            return cpuidVendor switch
            {
                "GenuineIntel" =>
                    "Intel Corporation",

                "AuthenticAMD" =>
                    "AMD",

                "ARM" =>
                    "ARM",

                _ =>
                    cpuidVendor
            };
        }

        // ==========================================================
        // CPUID
        // ==========================================================

        private readonly struct CpuIdFrequencyInfo
        {
            public CpuIdFrequencyInfo(
                uint baseMHz,
                uint maxTurboMHz,
                uint busMHz,
                bool supported)
            {
                BaseMHz = baseMHz;
                MaxTurboMHz = maxTurboMHz;
                BusMHz = busMHz;
                Supported = supported;
            }

            public uint BaseMHz { get; }

            public uint MaxTurboMHz { get; }

            public uint BusMHz { get; }

            public bool Supported { get; }
        }

        private static CpuIdFrequencyInfo GetCpuIdFrequencyInfo()
        {
            try
            {
                if (!X86Base.IsSupported)
                {
                    return new CpuIdFrequencyInfo(
                        0,
                        0,
                        0,
                        false);
                }

                (
                    int Eax,
                    int Ebx,
                    int Ecx,
                    int Edx
                ) leaf0 =
                    X86Base.CpuId(
                        0,
                        0);

                int highestStandardLeaf =
                    leaf0.Eax;

                if (highestStandardLeaf < 0x16)
                {
                    return new CpuIdFrequencyInfo(
                        0,
                        0,
                        0,
                        false);
                }

                (
                    int Eax,
                    int Ebx,
                    int Ecx,
                    int Edx
                ) leaf16 =
                    X86Base.CpuId(
                        0x16,
                        0);

                uint baseMHz =
                    (uint)(leaf16.Eax & 0xFFFF);

                uint maxMHz =
                    (uint)(leaf16.Ebx & 0xFFFF);

                uint busMHz =
                    (uint)(leaf16.Ecx & 0xFFFF);

                // Alguns processadores podem declarar a folha
                // mas retornar zeros. Nesse caso não consideramos
                // uma leitura útil.
                if (baseMHz == 0 &&
                    maxMHz == 0)
                {
                    return new CpuIdFrequencyInfo(
                        0,
                        0,
                        0,
                        false);
                }

                return new CpuIdFrequencyInfo(
                    baseMHz,
                    maxMHz,
                    busMHz,
                    true);
            }
            catch
            {
                return new CpuIdFrequencyInfo(
                    0,
                    0,
                    0,
                    false);
            }
        }

        // ==========================================================
        // MEMÓRIA RAM
        // ==========================================================

        private string ReadRam()
        {
            try
            {
                ulong totalRam = 0;

                using (ManagementObjectSearcher totalSearcher =
                    new ManagementObjectSearcher(
                        "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject system in totalSearcher.Get())
                    {
                        totalRam =
                            GetUInt64(
                                system,
                                "TotalPhysicalMemory");
                    }
                }

                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_PhysicalMemory");

                List<string> modules =
                    new List<string>();

                int slot = 1;

                foreach (ManagementObject ram in searcher.Get())
                {
                    ulong capacity =
                        GetUInt64(
                            ram,
                            "Capacity");

                    uint speed =
                        GetUInt32(
                            ram,
                            "Speed");

                    uint configuredSpeed =
                        GetUInt32(
                            ram,
                            "ConfiguredClockSpeed");

                    string manufacturer =
                        GetString(
                            ram,
                            "Manufacturer");

                    string partNumber =
                        GetString(
                            ram,
                            "PartNumber");

                    string serial =
                        GetString(
                            ram,
                            "SerialNumber");

                    string bank =
                        GetString(
                            ram,
                            "BankLabel");

                    string deviceLocator =
                        GetString(
                            ram,
                            "DeviceLocator");

                    string formFactor =
                        GetMemoryFormFactor(
                            GetUInt16(
                                ram,
                                "FormFactor"));

                    string memoryType =
                        GetMemoryType(
                            GetUInt16(
                                ram,
                                "SMBIOSMemoryType"));

                    string dataWidth =
                        GetString(
                            ram,
                            "DataWidth");

                    string totalWidth =
                        GetString(
                            ram,
                            "TotalWidth");

                    modules.Add(
                        "══════════════════════════════════════════════════════\r\n" +
                        $" MÓDULO DE MEMÓRIA {slot}\r\n" +
                        "══════════════════════════════════════════════════════\r\n\r\n" +

                        $"Capacidade................: {FormatBytes(capacity)}\r\n" +
                        $"Fabricante................: {manufacturer}\r\n" +
                        $"Part Number...............: {partNumber}\r\n" +
                        $"Número de série...........: {serial}\r\n" +
                        $"Slot......................: {deviceLocator}\r\n" +
                        $"Banco.....................: {bank}\r\n" +
                        $"Tipo......................: {memoryType}\r\n" +
                        $"Formato...................: {formFactor}\r\n" +
                        $"Velocidade................: {FormatMHz(speed)}\r\n" +
                        $"Velocidade configurada....: {FormatMHz(configuredSpeed)}\r\n" +
                        $"Data Width................: {dataWidth}\r\n" +
                        $"Total Width...............: {totalWidth}\r\n"
                    );

                    slot++;
                }

                string header =
                    "══════════════════════════════════════════════════════\r\n" +
                    " MEMÓRIA RAM DO COMPUTADOR\r\n" +
                    "══════════════════════════════════════════════════════\r\n\r\n" +

                    $"Memória RAM total.........: {FormatBytes(totalRam)}\r\n" +
                    $"Módulos detectados........: {modules.Count}\r\n\r\n";

                if (modules.Count == 0)
                {
                    return
                        header +
                        "Nenhum módulo de memória foi encontrado.";
                }

                return
                    header +
                    string.Join(
                        "\r\n",
                        modules);
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar a memória RAM:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // GPU
        // ==========================================================

        private string ReadGpu()
        {
            try
            {
                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_VideoController");

                List<string> gpus =
                    new List<string>();

                int index = 1;

                foreach (ManagementObject gpu in searcher.Get())
                {
                    string name =
                        GetString(gpu, "Name");

                    string manufacturer =
                        GetString(
                            gpu,
                            "AdapterCompatibility");

                    string videoProcessor =
                        GetString(
                            gpu,
                            "VideoProcessor");

                    ulong adapterRam =
                        GetUInt64(
                            gpu,
                            "AdapterRAM");

                    string driverVersion =
                        GetString(
                            gpu,
                            "DriverVersion");

                    string driverDate =
                        GetString(
                            gpu,
                            "DriverDate");

                    string pnpDeviceId =
                        GetString(
                            gpu,
                            "PNPDeviceID");

                    string deviceId =
                        GetString(
                            gpu,
                            "DeviceID");

                    string status =
                        GetString(
                            gpu,
                            "Status");

                    uint currentWidth =
                        GetUInt32(
                            gpu,
                            "CurrentHorizontalResolution");

                    uint currentHeight =
                        GetUInt32(
                            gpu,
                            "CurrentVerticalResolution");

                    uint refreshRate =
                        GetUInt32(
                            gpu,
                            "CurrentRefreshRate");

                    uint currentBits =
                        GetUInt32(
                            gpu,
                            "CurrentBitsPerPixel");

                    uint availability =
                        GetUInt16(
                            gpu,
                            "Availability");

                    string availabilityText =
                        GetAvailability(
                            availability);

                    gpus.Add(
                        "══════════════════════════════════════════════════════\r\n" +
                        $" GPU {index}\r\n" +
                        "══════════════════════════════════════════════════════\r\n\r\n" +

                        $"Nome........................: {name}\r\n" +
                        $"Fabricante..................: {manufacturer}\r\n" +
                        $"Processador gráfico.........: {videoProcessor}\r\n" +
                        $"Memória de vídeo...........: {FormatBytes(adapterRam)}\r\n" +
                        $"Driver......................: {driverVersion}\r\n" +
                        $"Data do driver..............: {FormatWmiDate(driverDate)}\r\n" +
                        $"Resolução atual.............: {currentWidth} x {currentHeight}\r\n" +
                        $"Taxa de atualização.........: {refreshRate} Hz\r\n" +
                        $"Profundidade de cor.........: {currentBits} bits\r\n" +
                        $"Status......................: {status}\r\n" +
                        $"Disponibilidade.............: {availabilityText}\r\n" +
                        $"Device ID...................: {deviceId}\r\n" +
                        $"PNP Device ID...............: {pnpDeviceId}\r\n"
                    );

                    index++;
                }

                return gpus.Count > 0
                    ? string.Join("\r\n\r\n", gpus)
                    : "Nenhuma GPU encontrada.";
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar a GPU:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // HD / SSD
        // ==========================================================

        private string ReadStorage()
        {
            try
            {
                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_DiskDrive");

                List<string> disks =
                    new List<string>();

                int index = 1;

                foreach (ManagementObject disk in searcher.Get())
                {
                    string model =
                        GetString(
                            disk,
                            "Model");

                    string manufacturer =
                        GetString(
                            disk,
                            "Manufacturer");

                    string serial =
                        GetString(
                            disk,
                            "SerialNumber");

                    string interfaceType =
                        GetString(
                            disk,
                            "InterfaceType");

                    string mediaType =
                        GetString(
                            disk,
                            "MediaType");

                    string firmwareRevision =
                        GetString(
                            disk,
                            "FirmwareRevision");

                    string deviceId =
                        GetString(
                            disk,
                            "DeviceID");

                    string pnpDeviceId =
                        GetString(
                            disk,
                            "PNPDeviceID");

                    string status =
                        GetString(
                            disk,
                            "Status");

                    ulong size =
                        GetUInt64(
                            disk,
                            "Size");

                    ulong partitions =
                        GetUInt32(
                            disk,
                            "Partitions");

                    string diskType =
                        DetermineStorageType(
                            model,
                            mediaType,
                            interfaceType);

                    disks.Add(
                        "══════════════════════════════════════════════════════\r\n" +
                        $" UNIDADE {index}\r\n" +
                        "══════════════════════════════════════════════════════\r\n\r\n" +

                        $"Modelo......................: {model}\r\n" +
                        $"Fabricante..................: {manufacturer}\r\n" +
                        $"Tipo........................: {diskType}\r\n" +
                        $"Interface...................: {interfaceType}\r\n" +
                        $"Mídia.......................: {mediaType}\r\n" +
                        $"Capacidade..................: {FormatBytes(size)}\r\n" +
                        $"Partições...................: {partitions}\r\n" +
                        $"Número de série.............: {serial}\r\n" +
                        $"Firmware....................: {firmwareRevision}\r\n" +
                        $"Status......................: {status}\r\n" +
                        $"Device ID...................: {deviceId}\r\n" +
                        $"PNP Device ID...............: {pnpDeviceId}\r\n"
                    );

                    index++;
                }

                return disks.Count > 0
                    ? string.Join("\r\n\r\n", disks)
                    : "Nenhum HD/SSD encontrado.";
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar HD/SSD:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // SISTEMA OPERACIONAL
        // ==========================================================

        private string ReadOperatingSystem()
        {
            try
            {
                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT * FROM Win32_OperatingSystem");

                foreach (ManagementObject os in searcher.Get())
                {
                    string caption =
                        GetString(
                            os,
                            "Caption");

                    string version =
                        GetString(
                            os,
                            "Version");

                    string build =
                        GetString(
                            os,
                            "BuildNumber");

                    string architecture =
                        GetString(
                            os,
                            "OSArchitecture");

                    string manufacturer =
                        GetString(
                            os,
                            "Manufacturer");

                    string serial =
                        GetString(
                            os,
                            "SerialNumber");

                    string registeredUser =
                        GetString(
                            os,
                            "RegisteredUser");

                    string organization =
                        GetString(
                            os,
                            "Organization");

                    string installDate =
                        FormatWmiDate(
                            GetString(
                                os,
                                "InstallDate"));

                    string lastBoot =
                        FormatWmiDate(
                            GetString(
                                os,
                                "LastBootUpTime"));

                    string windowsDirectory =
                        GetString(
                            os,
                            "WindowsDirectory");

                    string systemDirectory =
                        GetString(
                            os,
                            "SystemDirectory");

                    string countryCode =
                        GetString(
                            os,
                            "CountryCode");

                    string locale =
                        GetString(
                            os,
                            "Locale");

                    string status =
                        GetString(
                            os,
                            "Status");

                    return
                        "══════════════════════════════════════════════════════\r\n" +
                        " SISTEMA OPERACIONAL\r\n" +
                        "══════════════════════════════════════════════════════\r\n\r\n" +

                        $"Sistema....................: {caption}\r\n" +
                        $"Fabricante.................: {manufacturer}\r\n" +
                        $"Versão.....................: {version}\r\n" +
                        $"Build......................: {build}\r\n" +
                        $"Arquitetura................: {architecture}\r\n" +
                        $"Número de série............: {serial}\r\n" +
                        $"Usuário registrado........: {registeredUser}\r\n" +
                        $"Organização................: {organization}\r\n" +
                        $"Data de instalação........: {installDate}\r\n" +
                        $"Última inicialização.......: {lastBoot}\r\n" +
                        $"Diretório do Windows.......: {windowsDirectory}\r\n" +
                        $"Diretório do sistema.......: {systemDirectory}\r\n" +
                        $"Código do país.............: {countryCode}\r\n" +
                        $"Locale.....................: {locale}\r\n" +
                        $"Status.....................: {status}\r\n";
                }

                return
                    "Sistema operacional não encontrado.";
            }
            catch (Exception ex)
            {
                return
                    "Erro ao consultar o sistema operacional:\r\n" +
                    ex.Message;
            }
        }

        // ==========================================================
        // FUNÇÕES AUXILIARES
        // ==========================================================

        private static string GetString(
            ManagementBaseObject obj,
            string property)
        {
            try
            {
                object? value =
                    obj[property];

                if (value == null)
                    return "N/D";

                string result =
                    Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture)
                    ?? "N/D";

                return string.IsNullOrWhiteSpace(result)
                    ? "N/D"
                    : result.Trim();
            }
            catch
            {
                return "N/D";
            }
        }

        private static uint GetUInt32(
            ManagementBaseObject obj,
            string property)
        {
            try
            {
                object? value =
                    obj[property];

                if (value == null)
                    return 0;

                return Convert.ToUInt32(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static ushort GetUInt16(
            ManagementBaseObject obj,
            string property)
        {
            try
            {
                object? value =
                    obj[property];

                if (value == null)
                    return 0;

                return Convert.ToUInt16(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static ulong GetUInt64(
            ManagementBaseObject obj,
            string property)
        {
            try
            {
                object? value =
                    obj[property];

                if (value == null)
                    return 0;

                return Convert.ToUInt64(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static string FirstAvailable(
            params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value) &&
                    value != "N/D")
                {
                    return value;
                }
            }

            return "N/D";
        }

        private static string FormatBytes(
            ulong bytes)
        {
            if (bytes == 0)
                return "N/D";

            const double KB = 1024.0;
            const double MB = KB * 1024.0;
            const double GB = MB * 1024.0;
            const double TB = GB * 1024.0;

            if (bytes >= TB)
                return $"{bytes / TB:F2} TB";

            if (bytes >= GB)
                return $"{bytes / GB:F2} GB";

            if (bytes >= MB)
                return $"{bytes / MB:F2} MB";

            if (bytes >= KB)
                return $"{bytes / KB:F2} KB";

            return $"{bytes} bytes";
        }

        private static string FormatKB(
            ulong kb)
        {
            if (kb == 0)
                return "N/D";

            return $"{kb:N0} KB";
        }

        private static string FormatMHz(
            uint mhz)
        {
            if (mhz == 0)
                return "N/D";

            if (mhz >= 1000)
            {
                return
                    $"{mhz:N0} MHz " +
                    $"({mhz / 1000.0:F2} GHz)";
            }

            return $"{mhz:N0} MHz";
        }

        private static string FormatWmiDate(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value == "N/D")
            {
                return "N/D";
            }

            try
            {
                DateTime date =
                    ManagementDateTimeConverter.ToDateTime(
                        value);

                return date.ToString(
                    "dd/MM/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return value;
            }
        }

        private static string GetArchitecture(
            ushort architecture)
        {
            return architecture switch
            {
                0 => "x86",
                1 => "MIPS",
                2 => "Alpha",
                3 => "PowerPC",
                5 => "ARM",
                6 => "Itanium",
                9 => "x64",
                12 => "ARM64",
                _ => "N/D"
            };
        }

        private static string GetMemoryType(
            ushort type)
        {
            return type switch
            {
                0 => "Desconhecido",
                1 => "Outro",
                2 => "DRAM",
                3 => "Synchronous DRAM",
                4 => "Cache DRAM",
                5 => "EDO",
                6 => "EDRAM",
                7 => "VRAM",
                8 => "SRAM",
                9 => "RAM",
                10 => "ROM",
                11 => "Flash",
                12 => "EEPROM",
                13 => "FEPROM",
                14 => "EPROM",
                15 => "CDRAM",
                16 => "3DRAM",
                17 => "SDRAM",
                18 => "SGRAM",
                19 => "RDRAM",
                20 => "DDR",
                21 => "DDR2",
                22 => "DDR2 FB-DIMM",
                24 => "DDR3",
                26 => "DDR4",
                27 => "LPDDR",
                28 => "LPDDR2",
                29 => "LPDDR3",
                30 => "LPDDR4",
                34 => "DDR5",
                35 => "LPDDR5",
                _ => "N/D"
            };
        }

        private static string GetMemoryFormFactor(
            ushort value)
        {
            return value switch
            {
                0 => "Desconhecido",
                1 => "Outro",
                2 => "SIP",
                3 => "DIP",
                4 => "ZIP",
                5 => "SOJ",
                6 => "Proprietary",
                7 => "SIMM",
                8 => "DIMM",
                9 => "TSOP",
                10 => "PGA",
                11 => "RIMM",
                12 => "SODIMM",
                13 => "SRIMM",
                14 => "SMD",
                15 => "SSMP",
                16 => "QFP",
                17 => "TQFP",
                18 => "SOIC",
                19 => "LCC",
                20 => "PLCC",
                21 => "BGA",
                22 => "FPBGA",
                23 => "LGA",
                _ => "N/D"
            };
        }

        private static string GetAvailability(
            uint value)
        {
            return value switch
            {
                3 => "Em execução / funcionando",
                4 => "Degradado",
                5 => "Não aplicável",
                8 => "Offline",
                10 => "Degradado",
                11 => "Não instalado",
                _ => "N/D"
            };
        }

        private static string DetermineStorageType(
            string model,
            string mediaType,
            string interfaceType)
        {
            string text =
                $"{model} {mediaType} {interfaceType}"
                .ToUpperInvariant();

            if (text.Contains("NVME"))
                return "SSD NVMe";

            if (text.Contains("SSD") ||
                text.Contains("SOLID STATE"))
            {
                return "SSD";
            }

            if (text.Contains("HARD DISK"))
                return "HD";

            if (text.Contains("SATA"))
                return "HD/SSD SATA";

            return "HD/SSD - tipo não identificado";
        }
    }

    // ==============================================================
    // MODELO DE INFORMAÇÕES
    // ==============================================================

    public class PcInformation
    {
        public string Motherboard { get; set; } = "";

        public string Cpu { get; set; } = "";

        public string Ram { get; set; } = "";

        public string Gpu { get; set; } = "";

        public string Storage { get; set; } = "";

        public string OperatingSystem { get; set; } = "";
    }
}