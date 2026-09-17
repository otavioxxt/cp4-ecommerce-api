# CP4 — E-Commerce API (.NET 8)

API RESTful de **Produtos e Pedidos** desenvolvida para o CP4 da disciplina
**Advanced Business Development with .NET** — FIAP, turma 2TDSR, 2026.

---

## 👥 Integrantes

| Nome completo | RM | Turma |
| --- | --- | --- |
| Nickolas Davi | RM564105 | 2TDSR |
| Samara Vilela de Oliveira | RM566133 | 2TDSR |
| Natália Cristina de Souza | RM564099 | 2TDSR |
| Otávio Ferreira | RM565960 | 2TDSR |
| Rodrigo Carvalho Silva | RM565162 | 2TDSR |

**Tema:** E-Commerce — Produtos e Pedidos

---

## 📌 Descrição do projeto

O domínio tem três entidades:

- **ProdutoEntity** — item do catálogo, com preço, estoque, categoria e exclusão lógica.
- **PedidoEntity** — pedido do cliente, com número, status, valor total e seus itens.
- **ItemPedidoEntity** — item do pedido, que grava o preço unitário no momento da
  venda, para que reajustes no catálogo não alterem o histórico.

### Regras de negócio (nos UseCases)

| Regra | Onde está |
| --- | --- |
| Nome do produto é único | `ProdutoUseCase.AdicionarProdutoAsync` |
| Preço maior que zero e estoque não negativo | `ProdutoUseCase.ValidarProduto` |
| Estoque nunca fica negativo | `PedidoUseCase.BaixarEstoque` |
| Produto inativo não pode ser vendido | `PedidoUseCase.BaixarEstoque` |
| Produto repetido soma quantidade em vez de duplicar o item | `PedidoUseCase.AdicionarItemAsync` |
| Pedido confirmado não aceita alteração de itens | `PedidoUseCase.AdicionarItemAsync` |
| Pedido sem itens não pode ser confirmado | `PedidoUseCase.ConfirmarPedidoAsync` |
| Pedido entregue não pode ser cancelado | `PedidoUseCase.CancelarPedidoAsync` |
| Cancelamento devolve as unidades ao estoque | `PedidoUseCase.CancelarPedidoAsync` |

---

## 🏛️ Arquitetura

```
CP4.ECommerce.sln
├── Presentation
│   └── CP4.ECommerce.API
│       ├── Controllers/       ProdutoController, PedidoController, HealthController
│       ├── Doc/Samples/       exemplos do Swagger (IExamplesProvider)
│       └── Program.cs
│
├── Application
│   └── CP4.ECommerce.Application
│       ├── Dtos/              ProdutoDto, PedidoDto, ItemPedidoDto (records)
│       ├── Interfaces/        IProdutoUseCase, IPedidoUseCase
│       ├── Mappers/           ProdutoMapper, PedidoMapper (extension methods)
│       └── UseCases/          ProdutoUseCase, PedidoUseCase
│
├── Domain
│   └── CP4.ECommerce.Domain
│       ├── Entities/          ProdutoEntity, PedidoEntity, ItemPedidoEntity
│       ├── Interfaces/        IProdutoRepository, IPedidoRepository
│       └── Models/            PageResultModel<T>
│
├── Infrastructure
│   └── CP4.ECommerce.Infrastructure
│       ├── Data/AppData/      ApplicationContext
│       ├── Data/Repositories/ ProdutoRepository, PedidoRepository
│       ├── Data/Seed/         DataSeeder
│       └── Ioc/               Bootstrap.cs
│
└── Tests
    ├── CP4.ECommerce.Tests.Unit        Repository (InMemory) + UseCase (Moq) + Mapper
    └── CP4.ECommerce.Tests.Functional  Controller (WebApplicationFactory)
```

| Camada | Papel |
| --- | --- |
| **Presentation** | Recebe a requisição, valida o payload e chama a Application. Sem regra de negócio. |
| **Application** | Orquestra os casos de uso e converte DTO → Entidade pelos Mappers. Depende só do Domain. |
| **Domain** | Entidades e contratos de repositório. Não conhece banco nem API. |
| **Infrastructure** | `ApplicationContext`, repositórios concretos e IoC. |

A Application depende de `IProdutoRepository` / `IPedidoRepository`, declaradas no
**Domain**. A implementação concreta vive na **Infrastructure** e é injetada pelo
`Bootstrap.cs` — essa inversão de dependência é o que permite testar os UseCases
com o repositório mockado, sem banco.

---

## ⚙️ Configurações

Tudo em `Presentation/CP4.ECommerce.API/appsettings.json`.

### Banco de dados Oracle

```json
"ConnectionStrings": {
  "Oracle": "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
}
```

> Troque `SEU_RM` e `SUA_SENHA` pelas credenciais da FIAP.

As tabelas são criadas e populadas na subida da aplicação (`EnsureCreated` + `DataSeeder`),
controlado por `"Database": { "InicializarNaSubida": true }`.

### Application Insights

```json
"ApplicationInsights": {
  "Habilitado": true,
  "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://brazilsouth-1.in.applicationinsights.azure.com/"
}
```

A connection string sai do portal do Azure (*Overview → Connection String*). A telemetria
é enviada por `AddOpenTelemetry().UseAzureMonitor(...)`, que captura requests, dependências
(incluindo as queries do EF Core) e os logs do `ILogger`.

---

## ▶️ Como rodar

Pré-requisito: **.NET SDK 8.0**.

```bash
dotnet restore
dotnet build
dotnet run --project Presentation/CP4.ECommerce.API
```

Swagger em **<http://localhost:5080/swagger>** (a raiz `/` redireciona para lá).

### Testes

```bash
dotnet test
```

> Os testes **não precisam do Oracle nem da VPN da FIAP**: os de repositório usam o
> provedor **InMemory** do EF Core e os funcionais sobem a API com `WebApplicationFactory`
> substituindo os UseCases por **Mocks**.

---

## 🧪 Testes automatizados

### Repositório — EF Core InMemory

`Tests/CP4.ECommerce.Tests.Unit/APP/ProdutoRepositoryTest.cs` e `PedidoRepositoryTest.cs`

```csharp
var options = new DbContextOptionsBuilder<ApplicationContext>()
    .UseInMemoryDatabase(databaseName: $"TestDatabase_Produto_{Guid.NewGuid()}")
    .Options;
```

Validam paginação (`Skip`/`Take`), filtros, CRUD, exclusão lógica, nome duplicado e o
carregamento dos itens do pedido com `Include`.

### UseCase — Moq

`ProdutoUseCaseTest.cs`, `PedidoUseCaseTest.cs` e `MapperTest.cs`

```csharp
_produtoRepository = new Mock<IProdutoRepository>();
_produtoUseCase = new ProdutoUseCase(_produtoRepository.Object, NullLogger<ProdutoUseCase>.Instance);

_produtoRepository
    .Setup(obj => obj.ExisteNomeAsync(dto.Nome, null))
    .Returns(Task.FromResult(true));
```

Validam as regras de negócio da tabela acima e os Mappers.

### Controller — WebApplicationFactory

`Tests/CP4.ECommerce.Tests.Functional/`

```csharp
builder.ConfigureServices(services =>
{
    services.RemoveAll(typeof(IProdutoUseCase));
    services.AddSingleton(ProdutoUseCaseMock.Object);
});
```

Validam 200, 204, 400, 404, 201 com `Location`, HATEOAS, compressão Brotli/Gzip,
health check e o `swagger.json`.

Organização no Test Explorer por `[Trait]`: `Repository`, `UseCase`, `Mapper`,
`Controller` e `Infra`.

---

## 📈 Paginação e índices

### Paginação Offset-Based

```
GET /api/produto?deslocamento=3&registroRetornado=3&categoria=Monitores
```

```json
{
  "data": {
    "data": [ /* produtos */ ],
    "deslocamento": 3,
    "registroRetornado": 3,
    "totalRegistros": 48
  },
  "links": { "self": "/api/produto", "getById": "/api/produto/9" }
}
```

O `PageResultModel<T>` usa generics para assumir o tipo do objeto paginado. A paginação
é executada no banco (`Skip`/`Take` → `OFFSET … FETCH NEXT`), `registroRetornado` é
limitado a 50 e as listagens usam `AsNoTracking()`.

### Índices

Declarados por atributo, nas entidades:

```csharp
[Table("tb_produto")]
[Index(nameof(Nome), IsUnique = true, Name = "IDX_produto_nome")]                 // Único
[Index(nameof(Categoria), Name = "IDX_produto_categoria")]                        // Simples
[Index(nameof(Categoria), nameof(Ativo), Name = "IDX_produto_categoria_ativo")]   // Composto
```

| Índice | Colunas | Consulta que atende |
| --- | --- | --- |
| `IDX_produto_nome` (único) | `tb_produto (Nome)` | busca por nome e unicidade |
| `IDX_produto_categoria` | `tb_produto (Categoria)` | filtro por categoria |
| `IDX_produto_categoria_ativo` | `tb_produto (Categoria, Ativo)` | produtos ativos de uma categoria |
| `IDX_pedido_numero` (único) | `tb_pedido (NumeroPedido)` | busca pelo número do pedido |
| `IDX_pedido_email` | `tb_pedido (ClienteEmail)` | histórico do cliente |
| `IDX_pedido_status_data` | `tb_pedido (Status, DataPedido)` | listagem por status |
| `IDX_item_pedido` | `tb_item_pedido (PedidoId)` | carga dos itens de um pedido |
| `IDX_item_produto` (único) | `tb_item_pedido (PedidoId, ProdutoId)` | impede produto repetido no pedido |

---

## 🛡️ Performance e proteção

### Compressão de dados

Brotli e Gzip em `CompressionLevel.Fastest` — em API a diferença de bytes para `Optimal`
é pequena, mas o custo de CPU é grande.

```bash
curl -s -o /dev/null -w "%{size_download} bytes\n" \
  "http://localhost:5080/api/produto?registroRetornado=50"

curl -s -H "Accept-Encoding: br" -o /dev/null -w "%{size_download} bytes\n" \
  "http://localhost:5080/api/produto?registroRetornado=50"
```

### Rate Limiting

Duas políticas de janela fixa, **particionadas por IP do cliente** — cada IP tem a
sua própria cota, então um cliente que estoura o limite não afeta os demais:

| Política | Limite por IP |
| --- | --- |
| `rateLimitePolicy` | 5 requisições / 10 s, fila de 2 |
| `rateLimitePolicy2` | 3 requisições / 5 s, fila de 2 |

Ao exceder, a API responde **`429 Too Many Requests`**:

```bash
for i in $(seq 1 15); do
  curl -s -o /dev/null -w "%{http_code} " "http://localhost:5080/api/produto?registroRetornado=1"
done
# 200 200 200 200 200 429 429 429 429 429 ...
```

---

## 🩺 Observabilidade

### Health Checks

| Endpoint | Tipo | Verifica |
| --- | --- | --- |
| `GET /health` | Geral | todos os checks registrados |
| `GET /api/health/live` | Liveness | só o processo da API |
| `GET /api/health/db` | Readiness | conectividade com o Oracle |
| `GET /health/live` e `/health/db` | — | os mesmos checks por *minimal API* |

Com o banco fora do ar, o readiness responde **503**, sinal que o load balancer usa para
retirar a instância do pool.

### Logging estruturado

Serilog no console e em `logs/api-<data>.log`, rotação diária e retenção de 7 dias.
São registrados: consultas ao repositório (`Information`), cadastro e alteração
(`Information`), cancelamentos e "não encontrado" (`Warning`) e exceções (`Error`).

---

## 🔌 Endpoints

Base: `http://localhost:5080`

### Produtos

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/produto` | lista paginada, filtro opcional por `categoria` |
| `GET` | `/api/produto/{id}` | detalhe do produto |
| `POST` | `/api/produto` | cadastra produto |
| `PUT` | `/api/produto/{id}` | atualiza produto |
| `DELETE` | `/api/produto/{id}` | inativa produto (exclusão lógica) |

### Pedidos

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/pedido` | lista paginada, filtro opcional por `status` |
| `GET` | `/api/pedido/{id}` | pedido completo, com itens |
| `POST` | `/api/pedido` | abre pedido (status `Rascunho`) |
| `POST` | `/api/pedido/{id}/item` | adiciona item e baixa o estoque |
| `PATCH` | `/api/pedido/{id}/confirmar` | confirma o pedido |
| `PATCH` | `/api/pedido/{id}/cancelar` | cancela e repõe o estoque |

---

## 📋 Exemplos de requisição

### Cadastrar produto

```http
POST /api/produto
Content-Type: application/json

{
  "nome": "Teclado Mecanico RGB",
  "descricao": "Switch blue, layout ABNT2",
  "categoria": "Perifericos",
  "preco": 349.90,
  "estoque": 25
}
```

`201 Created` · `Location: /api/produto/49`

```json
{
  "id": 49,
  "nome": "Teclado Mecanico RGB",
  "descricao": "Switch blue, layout ABNT2",
  "categoria": "Perifericos",
  "preco": 349.90,
  "estoque": 25,
  "ativo": true,
  "dataCadastro": "2026-09-16T10:30:00"
}
```

### Abrir pedido com itens

```http
POST /api/pedido
Content-Type: application/json

{
  "clienteNome": "Otavio Santos",
  "clienteEmail": "otavio@exemplo.com",
  "itens": [
    { "produtoId": 49, "quantidade": 2 },
    { "produtoId": 10, "quantidade": 1 }
  ]
}
```

`201 Created`

```json
{
  "id": 16,
  "numeroPedido": "PED-20260916104500-A1B2C3",
  "clienteNome": "Otavio Santos",
  "clienteEmail": "otavio@exemplo.com",
  "status": "Rascunho",
  "valorTotal": 2599.70,
  "dataPedido": "2026-09-16T10:45:00",
  "itens": [
    { "id": 40, "pedidoId": 16, "produtoId": 49, "produtoNome": "Teclado Mecanico RGB", "quantidade": 2, "precoUnitario": 349.90, "subtotal": 699.80 },
    { "id": 41, "pedidoId": 16, "produtoId": 10, "produtoNome": "Monitor 27 QHD", "quantidade": 1, "precoUnitario": 1899.90, "subtotal": 1899.90 }
  ]
}
```

### Erro de regra de negócio

```http
POST /api/pedido/16/item
Content-Type: application/json

{ "produtoId": 49, "quantidade": 999 }
```

`400 Bad Request`

```json
{
  "mensagem": "Estoque insuficiente para o produto 'Teclado Mecanico RGB'. Disponível: 23, solicitado: 999."
}
```

---

## ✅ Checklist do CP4

| Requisito | Onde está |
| --- | --- |
| Organização em camadas | `Presentation`, `Application`, `Domain`, `Infrastructure` |
| Repository Pattern | `Domain/Interfaces` + `Infrastructure/Data/Repositories` |
| DTOs e mapeamentos | `Application/Dtos` (records) + `Application/Mappers` |
| Paginação | `PageResultModel<T>`, `Skip`/`Take` nos repositórios |
| Índices de banco | 8 índices por `[Index]` nas entidades |
| Compressão de dados | `AddResponseCompression` — Brotli e Gzip, `Fastest` |
| Rate Limit | `AddPolicy` + `RateLimitPartition` — janela fixa por IP, retorno 429 |
| Swagger Annotations | `[SwaggerOperation]`, `[SwaggerResponse]`, `[SwaggerParameter]` e `Doc/Samples` |
| Testes de unidade | `Tests/CP4.ECommerce.Tests.Unit` |
| Testes funcionais | `Tests/CP4.ECommerce.Tests.Functional` |
| Logging estruturado | Serilog na `Program.cs` + `ILogger` nos UseCases e controllers |
| Health Checks | `/health`, `/health/live`, `/health/db`, `/api/health/live` e `/api/health/db` |
| Tracing e métricas | `AddOpenTelemetry().UseAzureMonitor(...)` |
| README | este arquivo |
