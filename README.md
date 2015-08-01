NWNMasterServer  Istallazione

- Requisiti
-- .Net framework >= 4.0
-- MySQL server

Descrizione

Il programma è un riampiazzo del master server di bioware per la gestione degli accounts,
scaricare il sorgente e compilarlo con visual studio 2012 (nella cartella bin/debug è presente un eseguibile gia pronto per l'uso)

Il programma va installato come servizio di windows, usando l'utility di installazione locata in <PATH-WINDOWS-DIR>/Microsoft.NET/Framework/v4.0.30319/InstallUtil.exe <Path-eseguibile-server> (Framework64 in caso di sistemi a 64 bit)

Il programma necessita di una odbc di sistema chiamata "nwn_master_server" che punta al database dove salvare e prelevare gli accounts (possibilmente il database che viene utilizzato dal vostro server di nwn tramite nwnx).

All'interno del database va creata una tabella "accounts" cosi composta 

- id int(8) primary key auto_increment
- username varchar(32) unique
- password char(32) not null

E' stato testato su mysql 5, per scaricare mysql server e connettore odbc visitare il sito

I clients potrebbero non aver bisogno di modificare il file hosts per reindirizzare il login al vostro master server, questo comporterà che dopo aver inserito le credenziali nella sezione multiplayer spunti il messaggio "Impossibile raggiunge il master server", comunque per loggarre sul vostro server di gioco devono aver comunque fornito delle credenziali autorizzate altrimenti saranno respinti.

Per raggiungere il master server aggiungere la seguente riga al file hosts (Windows/System32/drivers/etc/hosts) (supponendo che giri sulla stessa macchina di nwserver)

127.0.0.1    nwmaster.bioware.com


Funzionamento

Attualmente gli account non vengono registrati dal master server, per registrarli inserirli manualmente (la password deve essere un hash a 32 caratteri md5) oppure registrarli al primo login tramite nwscript dando la possibilità di impostare la password

In seguito verrà aggiunto il sistema di creazione account
