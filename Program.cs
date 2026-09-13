using System;
using Microsoft.Data.SqlClient;

namespace TrabalhoFaculdadeImportCell
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. String de conexão para o 'master' (necessária para criar/dropar o banco do zero)
            string connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            // 2. Script para resetar o Banco de Dados
            string scriptCriarBanco = @"
                IF EXISTS (SELECT * FROM sys.databases WHERE name = 'ImportCell')
                BEGIN
                    ALTER DATABASE ImportCell SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE ImportCell;
                END;
                CREATE DATABASE ImportCell;
            ";

            // 3. Estrutura completa de Tabelas e Índices
            string scriptTabelas = @"
                CREATE TABLE Usuarios (
                    UsuarioID INT PRIMARY KEY IDENTITY(1,1),
                    Nome NVARCHAR(150) NOT NULL,
                    Email NVARCHAR(150) UNIQUE NOT NULL,
                    Senha NVARCHAR(255) NOT NULL,
                    Telefone NVARCHAR(20),
                    Endereco NVARCHAR(300),
                    Cidade NVARCHAR(100),
                    CEP NVARCHAR(10),
                    DataCadastro DATETIME DEFAULT GETDATE(),
                    Ativo BIT DEFAULT 1
                );

                CREATE TABLE Produtos (
                    ProdutoID INT PRIMARY KEY IDENTITY(1,1),
                    Nome NVARCHAR(150) NOT NULL,
                    Descricao NVARCHAR(500),
                    ImagemBase NVARCHAR(500),
                    PrecBase DECIMAL(10,2),
                    Marca NVARCHAR(100),
                    DataCadastro DATETIME DEFAULT GETDATE(),
                    Ativo BIT DEFAULT 1
                );

                CREATE TABLE Cores (
                    CorID INT PRIMARY KEY IDENTITY(1,1),
                    ProdutoID INT NOT NULL,
                    NomeCor NVARCHAR(100) NOT NULL,
                    CodigoHex NVARCHAR(10),
                    ImagemCor NVARCHAR(500),
                    FOREIGN KEY (ProdutoID) REFERENCES Produtos(ProdutoID) ON DELETE CASCADE
                );

                CREATE TABLE Armazenamento (
                    ArmazenamentoID INT PRIMARY KEY IDENTITY(1,1),
                    ProdutoID INT NOT NULL,
                    Tamanho NVARCHAR(50) NOT NULL,
                    Preco DECIMAL(10,2) NOT NULL,
                    Estoque INT DEFAULT 0,
                    FOREIGN KEY (ProdutoID) REFERENCES Produtos(ProdutoID) ON DELETE CASCADE
                );

                CREATE TABLE Carrinho (
                    CarrinhoID INT PRIMARY KEY IDENTITY(1,1),
                    UsuarioID INT NOT NULL,
                    DataCriacao DATETIME DEFAULT GETDATE(),
                    DataAtualizacao DATETIME DEFAULT GETDATE(),
                    FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE
                );

                CREATE TABLE ItensCarrinho (
                    ItemCarrinhoID INT PRIMARY KEY IDENTITY(1,1),
                    CarrinhoID INT NOT NULL,
                    ProdutoID INT NOT NULL,
                    CorID INT NOT NULL,
                    ArmazenamentoID INT NOT NULL,
                    Quantidade INT DEFAULT 1,
                    PrecoUnitario DECIMAL(10,2),
                    DataAdicao DATETIME DEFAULT GETDATE(),
                    FOREIGN KEY (CarrinhoID) REFERENCES Carrinho(CarrinhoID) ON DELETE CASCADE,
                    FOREIGN KEY (ProdutoID) REFERENCES Produtos(ProdutoID),
                    FOREIGN KEY (CorID) REFERENCES Cores(CorID),
                    FOREIGN KEY (ArmazenamentoID) REFERENCES Armazenamento(ArmazenamentoID)
                );

                CREATE TABLE Pedidos (
                    PedidoID INT PRIMARY KEY IDENTITY(1,1),
                    UsuarioID INT NOT NULL,
                    DataPedido DATETIME DEFAULT GETDATE(),
                    DataEntrega DATETIME,
                    EnderecoEntrega NVARCHAR(300) NOT NULL,
                    CidadeEntrega NVARCHAR(100),
                    CEPEntrega NVARCHAR(10),
                    Subtotal DECIMAL(10,2),
                    Frete DECIMAL(10,2) DEFAULT 15.00,
                    Total DECIMAL(10,2),
                    Status NVARCHAR(50) DEFAULT 'Pendente',
                    FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID)
                );

                CREATE TABLE ItensPedido (
                    ItemPedidoID INT PRIMARY KEY IDENTITY(1,1),
                    PedidoID INT NOT NULL,
                    ProdutoID INT NOT NULL,
                    CorID INT NOT NULL,
                    ArmazenamentoID INT NOT NULL,
                    Quantidade INT,
                    PrecoUnitario DECIMAL(10,2),
                    Subtotal DECIMAL(10,2),
                    FOREIGN KEY (PedidoID) REFERENCES Pedidos(PedidoID) ON DELETE CASCADE,
                    FOREIGN KEY (ProdutoID) REFERENCES Produtos(ProdutoID),
                    FOREIGN KEY (CorID) REFERENCES Cores(CorID),
                    FOREIGN KEY (ArmazenamentoID) REFERENCES Armazenamento(ArmazenamentoID)
                );

                CREATE TABLE Pagamentos (
                    PagamentoID INT PRIMARY KEY IDENTITY(1,1),
                    PedidoID INT NOT NULL UNIQUE,
                    MetodoPagamento NVARCHAR(50),
                    Status NVARCHAR(50) DEFAULT 'Pendente',
                    DataPagamento DATETIME,
                    Valor DECIMAL(10,2),
                    FOREIGN KEY (PedidoID) REFERENCES Pedidos(PedidoID) ON DELETE CASCADE
                );

                CREATE INDEX IDX_Usuarios_Email ON Usuarios(Email);
                CREATE INDEX IDX_Produtos_Ativo ON Produtos(Ativo);
                CREATE INDEX IDX_Cores_Produto ON Cores(ProdutoID);
                CREATE INDEX IDX_Armazenamento_Produto ON Armazenamento(ProdutoID);
            ";

            // 4. Inserção de TODOS os seus dados originais
            string scriptDados = @"
                INSERT INTO Usuarios (Nome, Email, Senha, Telefone, Endereco, Cidade, CEP) VALUES 
                    ('João Silva', 'joao@email.com', 'senha123', '11999999999', 'Rua A, 123', 'São Paulo', '01310-100'),
                    ('Maria Santos', 'maria@email.com', 'senha456', '11988888888', 'Rua B, 456', 'São Paulo', '01310-200'),
                    ('Pedro Costa', 'pedro@email.com', 'senha789', '11977777777', 'Rua C, 789', 'Rio de Janeiro', '20040020');

                INSERT INTO Produtos (Nome, Descricao, PrecBase, Marca) VALUES 
                    ('iPhone 16', 'Câmera dupla de 48 MP', 9499, 'Apple'),
                    ('Samsung Z Flip 6', 'Câmera profissional de 50MP', 5899, 'Samsung'),
                    ('Xiaomi Poco X7', 'Tela AMOLED de 6,67', 3299, 'Xiaomi'),
                    ('Samsung Galaxy S24 Ultra', 'Abertura brilhante F1.4', 6000, 'Samsung'),
                    ('iPhone 17 Pro Max', 'Sistema de câmera Pro avançado', 12000, 'Apple'),
                    ('OPPO Find X9 Pro', 'Telefoto revolucionária de 200MP', 9200, 'OPPO'),
                    ('OPPO Reno 14', 'Certificações IP66/68/69 lenda', 3999, 'OPPO'),
                    ('Xiaomi 17 Pro Max', 'Snapdragon 8 Elite premium', 6400, 'Xiaomi'),
                    ('iPhone 17e', 'Modelo focado em custo-benefício', 4999, 'Apple'),
                    ('iPhone 17 Air', 'Design ultrafino de 5,6 mm', 7199, 'Apple'),
                    ('Samsung Galaxy S25', 'Compacto com Snapdragon 8 Elite', 3889, 'Samsung'),
                    ('REDMAGIC 11 Pro', 'Gamer, bateria de 7000mAh', 5000, 'REDMAGIC');

                INSERT INTO Cores (ProdutoID, NomeCor, CodigoHex) VALUES 
                    (1, 'Rosa', '#FF00FF'), (1, 'Azul', '#63B8FF'), (1, 'Preto Espacial', '#111111'),
                    (2, 'Azul', '#BBFFFF'), (2, 'Preto', '#000000'),
                    (3, 'Preto', '#111111'), (3, 'Verde', '#98FB98'),
                    (4, 'Preto', '#111111'), (4, 'Cinza', '#BEBEBE'),
                    (5, 'Laranja', '#FFA500'), (5, 'Prateado', '#CDC9C9'),
                    (6, 'Vinho', '#8B0000'), (6, 'Grafite', '#8B8970'),
                    (7, 'Preto', '#000000'),
                    (8, 'Roxo', '#DDA0DD'), (8, 'Verde', '#8FBC8F'), (8, 'Preto', '#000000'),
                    (9, 'Rosa', '#FFC0CB'), (9, 'Branco', '#FFF5EE'), (9, 'Preto', '#000000'),
                    (10, 'Azul céu', '#CAE1FF'), (10, 'Branco', '#FFF5EE'), (10, 'Preto', '#000000'),
                    (11, 'Azul', '#0000FF'), (11, 'Branco', '#FFF5EE'),
                    (12, 'Branco', '#FFF5EE'), (12, 'Preto', '#000000');

                INSERT INTO Armazenamento (ProdutoID, Tamanho, Preco, Estoque) VALUES 
                    (1, '128GB', 6799, 10), (1, '256GB', 9499, 15), (1, '512GB', 10599, 8),
                    (2, '256GB', 5899, 12), (2, '512GB', 6599, 10),
                    (3, '256GB', 3299, 20), (3, '512GB', 3799, 15),
                    (4, '256GB', 6000, 12), (4, '512GB', 8000, 10),
                    (5, '256GB', 12000, 5), (5, '512GB', 14000, 5),
                    (6, '512GB', 9200, 8), (6, '1TB', 10000, 6),
                    (7, '256GB', 4000, 20),
                    (8, '512GB', 6470, 12),
                    (9, '256GB', 4299, 15),
                    (10, '256GB', 7248, 10),
                    (11, '256GB', 3900, 18),
                    (12, '512GB', 5000, 10), (12, '1TB', 8900, 5);
            ";

            // 5. Consulta Avançada (Traz Produto + Cor + Armazenamento ao mesmo tempo)
            string queryConsulta = @"
                SELECT 
                    p.Nome AS Produto,
                    p.Marca,
                    c.NomeCor AS Cor,
                    a.Tamanho,
                    a.Preco,
                    a.Estoque
                FROM Produtos p
                INNER JOIN Cores c ON p.ProdutoID = c.ProdutoID
                INNER JOIN Armazenamento a ON p.ProdutoID = a.ProdutoID
                ORDER BY p.Marca, p.Nome, a.Preco;
            ";

            // Execução no Banco de Dados
            using (SqlConnection conexao = new SqlConnection(connectionString))
            {
                try
                {
                    conexao.Open();
                    
                    Console.WriteLine("⏳ [1/3] Expulsando usuários antigos e recriando a base ImportCell...");
                    using (SqlCommand cmd = new SqlCommand(scriptCriarBanco, conexao)) { cmd.ExecuteNonQuery(); }

                    // Aponta a conexão para o banco recém-criado
                    conexao.ChangeDatabase("ImportCell");

                    Console.WriteLine("⏳ [2/3] Mapeando a arquitetura de tabelas e índices...");
                    using (SqlCommand cmd = new SqlCommand(scriptTabelas, conexao)) { cmd.ExecuteNonQuery(); }

                    Console.WriteLine("⏳ [3/3] Semeando dados do catálogo de smartphones...");
                    using (SqlCommand cmd = new SqlCommand(scriptDados, conexao)) { cmd.ExecuteNonQuery(); }

                    Console.Clear();
                    Console.WriteLine("=========================================================================================");
                    Console.WriteLine("                  🚀 IMPORTCELL - CATÁLOGO DE PRODUTOS COMPLETO                          ");
                    Console.WriteLine("=========================================================================================");
                    Console.WriteLine("{0,-10} | {1,-25} | {2,-15} | {3,-10} | {4,-12} | {5,-8}", "MARCA", "MODELO", "COR", "ESPAÇO", "PREÇO", "ESTOQUE");
                    Console.WriteLine(new string('-', 90));

                    using (SqlCommand cmd = new SqlCommand(queryConsulta, conexao))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0,-10} | {1,-25} | {2,-15} | {3,-10} | R$ {4,-9:N2} | {5,-8}",
                                    reader["Marca"],
                                    reader["Produto"],
                                    reader["Cor"],
                                    reader["Tamanho"],
                                    reader["Preco"],
                                    reader["Estoque"]);
                            }
                        }
                    }
                    Console.WriteLine(new string('-', 90));
                    Console.WriteLine("✅ Banco de dados reconstruído e populado com sucesso!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Falha crítica no processo: {ex.Message}");
                }
            }

            Console.WriteLine("\nPressione qualquer tecla para encerrar...");
            Console.ReadKey();
        }
    }
}