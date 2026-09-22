// Apps Script da planilha do DishFace.
// Recebe o tempo de cada jogador (enviado pelo Planilha.cs do jogo) e grava na aba "Tempos".
//
// Como instalar (uma vez):
// 1. Crie uma planilha no Google Planilhas.
// 2. Extensões > Apps Script. Apague o que estiver lá e cole este arquivo inteiro. Salve.
// 3. Implantar > Nova implantação > tipo "App da Web".
//    Executar como: Eu.  Quem pode acessar: Qualquer pessoa.  > Implantar (e autorize).
// 4. Copie a URL que termina em /exec e cole no arquivo
//    Assets/StreamingAssets/planilha.txt do jogo (no jogo pronto: <Jogo>_Data/StreamingAssets/planilha.txt).
//
// Mudou este código? Implantar > Gerenciar implantações > editar (lápis) > Versão: Nova versão.
// Se não fizer isso, a URL continua rodando o código antigo.

var SENHA = "dishface-feira"; // igual à SENHA do Planilha.cs
var ABA = "Tempos";

// O jogo manda: senha, data, nome, contato, fase, segundos
function doPost(e) {
  var p = e.parameter;
  if (p.senha !== SENHA) {
    return texto("senha errada");
  }

  var aba = pegarAba();
  var segundos = Number(p.segundos);
  var trava = LockService.getScriptLock(); // dois jogadores terminando juntos
  trava.waitLock(10000);
  try {
    aba.appendRow([p.data, p.nome, "'" + p.contato, p.fase, segundos, formatar(segundos), new Date()]);
  } finally {
    trava.releaseLock();
  }
  return texto("ok");
}

function pegarAba() {
  var planilha = SpreadsheetApp.getActiveSpreadsheet();
  var aba = planilha.getSheetByName(ABA);
  if (!aba) {
    aba = planilha.insertSheet(ABA);
    aba.appendRow(["Data (jogo)", "Nome", "Celular ou e-mail", "Fase", "Segundos", "Tempo", "Recebido em"]);
    aba.setFrozenRows(1);
  }
  return aba;
}

// 83.4 -> "01:23.4"
function formatar(s) {
  s = Math.round(s * 10) / 10; // senão 59.96 viraria "00:60.0"
  var m = Math.floor(s / 60);
  var r = (s - m * 60).toFixed(1);
  return (m < 10 ? "0" : "") + m + ":" + (r < 10 ? "0" : "") + r;
}

function texto(t) {
  return ContentService.createTextOutput(t);
}
