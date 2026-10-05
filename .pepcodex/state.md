# PEP-Codex State

Objetivo: Aplicacao generica de controle de estoque em pasta separada para o repositorio `generic_inventory_v3`.
Stack: ASP.NET 8, SPA estatica em wwwroot, SQLite/EF Core, xUnit.
Status: Base v3 criada em `C:\Users\daviks\Desktop\geneeric`, sem dados locais versionados.
Ultima mudanca: Copiada base generica sem marcas especificas; referencias v2 trocadas para v3.
Arquivos-chave: Program.cs, Data/, Products/, Movements/, Employees/, Reminders/, Auth/, wwwroot/.
Validacao: `node --check wwwroot/app.js` passou; `dotnet test GenericInventory.sln --no-restore` passou; busca por marcas/v2/dado pessoal nao retornou ocorrencias.
Bloqueios: Nenhum no momento.
Proximo: Publicar no GitHub como `generic_inventory_v3`.
Decisoes: App nasce com banco vazio; admin bootstrap usa `admin@example.com`; segredos ficam fora do codigo.
