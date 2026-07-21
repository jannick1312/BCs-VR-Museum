## 1. Titelfolie – ca. 20 Sekunden

Hallo zusammen. Ich zeige heute den aktuellen Stand meiner Bachelorarbeit. In meinem Projekt entwickle ich einen Prototyp für ein virtuelles Museum. Darin sollen Bilder, Videos und 3D-Modelle nicht nur gesucht, sondern direkt in einer räumlichen Ausstellung erkundet werden können.

Bevor ich auf die Umsetzung eingehe, möchte ich zuerst kurz erklären, wieso dieses Projekt sinvoll ist.

## 2. Motivation – ca. 1 Minute

Der Ausgangspunkt ist VIRTUE, ein bestehendes System. VIRTUE hat bereits gezeigt, dass man Multimedia Retrieval gut mit Virtual Reality verbinden kann.

Das System passt aber nicht mehr ganz zur heutigen Situation. VIRTUE verwendet mit Cineast und Cottontail DB einen älteren Technologie-Stack. IMan sieht auch, dass sich das System hauptsächlich auf Bilder konzentriert. Moderne Sammlungen bestehen aber oft nicht nur aus Bildern, sondern auch aus Videos und 3D-Modellen. Gleichzeitig gibt es heute neue XR-Headsets, neue Interaktionsmöglichkeiten und unterschiedliche Plattformen.

Die zentrale Motivation ist deshalb, diese Grundidee von VIRTUE zu modernisieren. Das neue System soll multimodale Inhalte unterstützen und auf verschiedenen aktuellen XR-Geräten verwendet werden können.

## 3. Project idea – ca. 1 Minute

Die Grundidee dass man räumlich durch einen raum exponate ansehen kann bleibt.
Statt Cineast und Cottontail verwende ich vitrivr-engine und pgvector. Neben Bildern sollen auch Videos und 3D-Modelle unterstützt werden. Über OpenXR soll die Anwendung ausserdem auf mehreren Headsets laufen können.

Wenn ich in der Ausstellung ein interessantes Objekt finde, kann ich direkt von diesem Objekt aus nach weiteren ähnlichen Inhalten suchen.

Bevor ich die Ziele und den Ablauf genauer erkläre, kläre ich kurz ein paar Begriffe, die in der Präsentation immer wieder vorkommen.

## 4. Background: key terms – ca. 50 Sekunden

VR steht für Virtual Reality. Damit meine ich hier die vollständig virtuelle Museumsumgebung, die man durch ein Headset erlebt.

XR steht für Extended Reality und ist der allgemeinere Oberbegriff. Dazu gehören neben VR auch Augmented Reality und Mixed Reality.

OpenXR ist eine gemeinsame Schnittstelle für XR-Anwendungen. Sie vereinheitlicht den Zugriff auf Headsets, Controller, Tracking und verschiedene XR-Runtimes. Dadurch muss die grundlegende Anwendung nicht für jedes Gerät komplett neu entwickelt werden.

CLIP steht für Contrastive Language–Image Pre-training. Das Modell bildet Texte und Bilder als numerische Vektoren ab, die man miteinander vergleichen kann. Diese Vektoren nennt man Embeddings.

HUD steht für Head-up Display. In meiner Anwendung ist das die Anzeige im Sichtfeld, die zum Beispiel den Suchfortschritt oder Fehlermeldungen zeigt.

## 5. Three goals define the prototype – ca. 1 Minute

Das erste Ziel meines prototypen ist Retrieve. vitrivr-engine und pgvector sollen über die gespeicherten Embeddings semantisch ähnliche Inhalte finden.

Das zweite Ziel ist Experience. Die gefundenen Bilder, Videos und 3D-Modelle sollen nicht einfach als normale Trefferliste angezeigt werden. Die XR-Anwendung macht daraus interaktive Exponate in einer räumlichen Umgebung.

Das dritte Ziel ist Explore. Von einem Exponat aus kann direkt die nächste Ähnlichkeitssuche gestartet werden. Die Suche endet also nicht bei einer einzelnen Resultatliste, sondern kann immer weitergeführt werden.

Wichtig ist aber hier die Abgrenzung: Ich entwickle kein neues Embedding-Modell sonder verwende diese techologie in meinem Museum.

Diese drei Ziele führen zu einem gemeinsamen Konzept.

## 6. Similarity creates a continuous exploration loop – ca. 1 Minute

Am Anfang steht eine Textanfrage. Die semantische Suche liefert dazu verwandte Medien. Diese Medien werden danach nicht nur aufgelistet, sondern als räumliche Ausstellung präsentiert. Dort kann ein Exponat ausgewählt und direkt als Ausgangspunkt für die nächste Suche verwendet werden.

So entsteht der Loop, den man auf der Folie sieht: suchen, Inhalte räumlich erkunden, ein interessantes Objekt auswählen und damit weitersuchen.

Dabei bleibt das Grundprinzip für alle Medientypen gleich. Es spielt für die Interaktion keine Rolle, ob das Resultat ein Bild, ein Video oder ein 3D-Modell ist. Gleichzeitig gibt es zwei Einstiegspunkte: Ich kann mit Text beginnen oder direkt von einem bestehenden Exponat aus weitersuchen.

Damit das funktioniert, müssen Text und Medien in einer gemeinsamen Form vergleichbar sein. Genau dafür wird CLIP verwendet.

## 7. CLIP creates a shared comparison space – ca. 1:20 Minuten

Oben sieht man die Offline-Verarbeitung der Sammlung. Die Mediendateien werden einmal durch den CLIP-Bildencoder verarbeitet. Dabei entstehen numerische Vektoren, die anschliessend gespeichert werden.

Unten sieht man die Live-Suche. Nur die neue Textanfrage wird durch den CLIP-Textencoder geschickt. Das ergibt den Query-Vektor.

Der entscheidende Punkt ist der gemeinsame Vergleichsraum auf der rechten Seite. Bild- und Textencoder erzeugen Vektoren, die direkt miteinander verglichen werden können. Über ihre Ähnlichkeit lassen sich die gespeicherten Medien danach sortieren.

CLIP wurde kontrastiv mit passenden und nicht passenden Bild-Text-Paaren trainiert. Vereinfacht gesagt sollen passende Paare im Embedding-Raum näher zusammenliegen als unpassende.

Bei Bildern und einzelnen Video-Frames funktioniert dieser Ablauf direkt. Bei einem 3D-Modell braucht es dagegen noch einen Zwischenschritt.

## 8. Rendered views make 3D models searchable – ca. 1 Minute

CLIP kann Bilder und Texte verarbeiten, aber nicht direkt die Geometrie eines 3D-Meshes. Deshalb wird das Modell aus mehreren Kamerapositionen gerendert.

Diese 2D-Ansichten können anschliessend wie normale Bilder durch den CLIP-Bildencoder verarbeitet werden. Das Resultat sind mehrere View-Deskriptoren, die weiterhin mit dem ursprünglichen GLB-Modell verknüpft sind.

Bei einer Suche werden also die gerenderten Ansichten verglichen und nicht das Mesh selbst. Wenn eine Ansicht gut zur Anfrage passt, wird über die Verknüpfung das zugehörige GLB-Modell gefunden und in das Museum geladen.

Als Nächstes zeige ich zuerst den Aufbau des Systems auf einer abstrakten Ebene und danach die konkrete technische Umsetzung.

## 9. Three responsibilities shape the system – ca. 1:10 Minuten

Das System ist in drei Verantwortungsbereiche aufgeteilt.

Links liegt die XR Experience. Dort werden Eingaben, Navigation und Auswahl verarbeitet. Dieser Teil stellt ausserdem das Menü, die Räume und die Exponate dar.

In der Mitte liegt die Anwendungslogik. Sie nimmt die Absicht aus dem XR-Teil entgegen und orchestriert den eigentlichen Ablauf. Dazu gehören zum Beispiel das Validieren einer Anfrage, das Starten der Suche und das Begrenzen der Resultate.

Rechts liegen Retrieval und Medienbereitstellung. Dort werden die Embeddings verglichen, Resultate ermittelt und die Mediendateien entweder über lokale Pfade oder über HTTP bereitgestellt.

Der wichtige Punkt ist die Trennung dieser drei Bereiche. Die XR-Seite muss keine Details über das Retrieval-Backend kennen. Dadurch kann ich zum Beispiel die Suchlogik oder die Darstellung ändern, ohne gleichzeitig das ganze System umbauen zu müssen.

Jetzt gehe ich eine Ebene tiefer und zeige, wie der Retrieval-Teil konkret mit vitrivr umgesetzt ist.

## 10. One vitrivr request supports both search modes – ca. 1:20 Minuten

Oben sieht man zuerst den vereinfachten Ablauf. Eine Eingabe aus VR wird in ein Query-Objekt umgewandelt. Daraus wird der vitrivr-Request aufgebaut. Die Antwort wird anschliessend geparst, die Medien werden geladen und am Ende in VR platziert.

Die untere Zeile ist eine Vergrösserung des vitrivr-Requests. Ganz links sieht man die beiden möglichen Eingaben. Für eine normale Textsuche wird ein `TEXT`-Input verwendet. Wenn ich von einem bestehenden Exponat aus weitersuche, wird stattdessen dessen gespeicherter `FLOATVECTOR` verwendet.

Danach läuft in beiden Fällen dieselbe Pipeline. Die `clip`-Operation führt die eigentliche Suche aus. Über die `partOf`-Relationen können zusammengehörende Einträge erweitert werden.

Der entscheidende Punkt ist: Nur das Input Mapping ändert sich. Das Parsen der Antwort, das Laden der Medien und die Platzierung in VR bleiben gleich. Dadurch brauche ich für Textsuche und Result-to-Result-Suche nicht zwei getrennte Abläufe.

Diese Trennung setzt sich auch im Aufbau meines Codes fort.

## 11. Godot and vitrivr remain replaceable – ca. 1 Minute

Hier sieht man, wie der Code grob geschichtet ist.

Ganz oben liegt die Godot-Schicht mit den Controllern und Szenen. Sie greift aber nicht direkt auf vitrivr zu, sondern arbeitet über das Interface `IMuseumApplication`.

Darunter liegen die Application Use Cases für die Suche und die Servervalidierung. Core und Models enthalten die gemeinsamen Query- und Resultattypen. Erst die Infrastructure-Schicht kennt die konkreten Details von vitrivr und vom Laden der Medien. Die Factory setzt diese Teile an einer zentralen Stelle zusammen.

Der Vorteil ist, dass Godot nicht vom konkreten Request-Format abhängig ist. Wenn ich den vitrivr-Request anpasse, muss ich nicht gleichzeitig alle Godot-Skripte ändern. Umgekehrt könnte auch die XR-Darstellung ausgetauscht werden, ohne die gesamte Suchlogik neu zu schreiben.

Für die XR-Anwendung habe ich mich aus zwei praktischen Gründen für Godot entschieden.

## 12. Why Godot? – ca. 50 Sekunden

Der erste Grund sind die Godot XR Tools. Sie liefern bereits Bausteine für Bewegung, Interaktion, Greifen und Benutzeroberflächen in XR. Diese grundlegenden Funktionen musste ich deshalb nicht selbst von Grund auf neu bauen.

Der zweite Grund ist das OpenXR Vendors Plugin. Es vereinfacht die Integration unterschiedlicher Geräte. Für Quest und Focus gibt es passende gerätespezifische Konfigurationen. Unter Windows wird die reguläre OpenXR-Runtime verwendet, zum Beispiel wenn die Anwendung vom PC auf ein Headset gestreamt wird.

Controller- und Handsteuerung verwenden dabei dieselben Szenen und dieselbe Anwendungslogik. Ich brauche also nicht zwei getrennte Anwendungen für die beiden Eingabemethoden.


## 14. Entering the museum – ca. 1 Minute

Die Anwendung startet zuerst im XR-Menü. Dort wird die Serververbindung geprüft und optional das Tutorial durchgeführt. Erst danach kann das eigentliche Museum betreten werden.
`CanEnterMuseum` ist nur wahr, wenn die Serververbindung gültig ist und das Tutorial entweder deaktiviert oder bereits abgeschlossen wurde.

Solange man im Menü ist, bleibt das Museum ausgeblendet und die Bewegung ist deaktiviert. Die Konfiguration findet trotzdem direkt innerhalb von XR statt.

Beim Wechsel verschiebt der `PlatformSwitcher` das XR-Rig zum Startpunkt des Museums. Gleichzeitig werden die passende Umgebung aktiviert. Der bisherige Zustand des Museums bleibt dabei erhalten. Wenn man später wieder zurück ins Menü geht, wird die bestehende Ausstellung also nicht einfach gelöscht.

Nach dem Eintritt können die Suchresultate geladen und als Exponate dargestellt werden.

## 15. Media become exhibits – ca. 1:10 Minuten

Oben sieht man den Ablauf von den Suchresultaten bis zum fertigen Exponat.

Zuerst werden die Retrievables geparst. Doppelte Dateipfade werden entfernt, und die Anzahl wird auf die verfügbaren Plätze begrenzt. Danach werden die Dateien entweder lokal oder asynchron über HTTP geladen.

Anschliessend entscheidet die Platzierungsstrategie, wie das Medium dargestellt wird. Bilder und Videos kommen in vorbereitete Plätze an den Wänden. Die Rahmen werden dabei an das Seitenverhältnis angepasst. GLB-Modelle werden auf 3D-Plätzen instanziiert und anhand ihrer Bounds auf eine passende Grösse skaliert.

Am Ende ist jedes Resultat ein interaktives Exponat. Es speichert seinen Namen, den Pfad und den CLIP-Vektor. Dieser Vektor wird verwendet, wenn man über `Similar` die nächste Suche startet. Bei 3D-Modellen gibt es zusätzlich die Ansicht `Original Size`.

Damit ist der ganze Weg von der Suchanfrage bis zur nächsten Exploration geschlossen. Bevor ich das System zeige, ordne ich noch kurz den aktuellen Stand ein.

## 17. Demo – 5 bis 8 Minuten

Damit ist sichtbar, dass der technische Ablauf grundsätzlich funktioniert. Offen ist aber noch, wie verständlich und angenehm andere Personen die Bedienung finden. Dafür ist die Evaluation geplant.

## 18. Evaluation questionnaire – ca. 1 Minute

Die Evaluation ist noch nicht durchgeführt. Bisher habe ich dafür den Fragebogen vorbereitet, den man auf dieser Folie in drei Teile aufteilen kann.

Der erste Teil ist die System Usability Scale, kurz SUS. Sie besteht aus zehn standardisierten Aussagen zur allgemeinen Benutzerfreundlichkeit.

Der zweite Teil enthält Fragen, die direkt auf mein Projekt bezogen sind. Dabei geht es unter anderem um Navigation, Steuerung, wahrgenommene Reaktionsgeschwindigkeit, die Interaktion mit 3D-Modellen und die Gestaltung der Anwendung.

Im dritten Teil erfasse ich etwas Kontext, zum Beispiel die bisherige VR-Erfahrung, das verwendete Gerät und ob die Anwendung als Standalone-Version oder per Streaming genutzt wurde. Zusätzlich gibt es eine offene Frage für Verbesserungsvorschläge.

Zum Schluss bleiben noch die nächsten Arbeitsschritte.

## 19. Next steps – ca. 1 Minute

Als Nächstes möchte ich zuerst die langsamen Ladevorgänge bei bestimmten Medien untersuchen und verbessern. Danach soll das Streaming zu Apple Vision Pro integriert und getestet werden.

Anschliessend folgen die User Evaluation sowie der reproduzierbare Aufbau und die Dokumentation. Der letzte Punkt ist dann natürlich die Thesis selbst.

Vielen Dank. Ich freue mich auf eure Fragen und euer Feedback.
