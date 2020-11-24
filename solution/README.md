# Readme dokument IPK projektu č.2 (2019/2020)
## autor: David Rubý (xrubyd00)
K implementaci tohoto projektu byl použit programovací jazyk C# s externí knihovnou SharpPcap na zachycení všech příchozích i odchozích packetů

>### Překlad projektu:
> pro překlad projektu slouží nástroj Makefile.\
> v kořenové složce projektu (obsahuje soubory: Makefile, README.md, manula.pdf a adresář src) spusťte Makefile script: ``make build``.\
> Počkejte, než se proces dokončí

>### Spuštění aplikace:
> přejděte do složky s binárním souborem:\
> ``cd src/bin/Debug/netcoreapp3.1/``\
> Spustě aplikaci s administrátorkými právy:
>>#### Linux:
>> ``sudo ./ipk-sniffer``
>
>>#### Windows:
>> ``runas /user:administrator ipk-sniffer``

>### Argumenty aplikace:
> ``-h`` nebo ``--help``        zobrazí nápovědu\
> ``-i <interface>``            specifikuje interface, na kterém se má poslouchat\
> ``-p <port>``                 specifikuje port, na kterém se má poslouchat. Když není uvedent, poslouchá se na všech portech\
> ``-t`` nebo ``-tcp``          umožňuje poslouchat tcp packety\
> ``-u`` nebo ``--udp``         umožňuje poslouchat udp packety\
> ``-n <number>``               specifikuje, kolik packetů zobrazit. Když není zadán, předpokládá se 1 packet.
>
>Pokud není ``udp`` ani ``tcp`` specifikováno, uvažují se oba protokoly.