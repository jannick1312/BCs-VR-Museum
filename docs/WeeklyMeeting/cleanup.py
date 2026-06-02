import os

# erlaubte Endungen
allowed_extensions = (".tex", ".pdf")

# Ordner, in dem das Script selbst liegt
script_dir = os.path.dirname(os.path.abspath(__file__))

# Name des Scripts selbst
script_name = os.path.basename(__file__)


def clean_folder(folder):
    for entry in os.listdir(folder):
        path = os.path.join(folder, entry)

        # Script selbst überspringen
        if entry == script_name and path == os.path.join(script_dir, script_name):
            continue

        # Wenn Ordner → rekursiv weitergehen
        if os.path.isdir(path):
            clean_folder(path)

            # Optional: leeren Ordner löschen
            try:
                if not os.listdir(path):
                    os.rmdir(path)
                    print(f"Leerer Ordner gelöscht: {path}")
            except Exception as e:
                print(f"Fehler beim Löschen des Ordners {path}: {e}")

        # Wenn Datei → Endung prüfen
        elif os.path.isfile(path):
            if not entry.endswith(allowed_extensions):
                try:
                    os.remove(path)
                    print(f"Gelöscht: {path}")
                except Exception as e:
                    print(f"Fehler bei {path}: {e}")


# Start
clean_folder(script_dir)