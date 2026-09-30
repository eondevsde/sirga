import fitz
import sys

def analyze_pdf(path):
    print(f"\n--- Analizando: {path} ---")
    try:
        doc = fitz.open(path)
        for i, page in enumerate(doc):
            print(f"Pagina {i+1} Texto:")
            print(page.get_text())
            print("-" * 20)
            
            # Intentar identificar tablas por su estructura de bloques si es posible
            tabs = page.find_tables()
            if tabs:
                print(f"Se encontraron {len(tabs.tables)} tablas.")
                for j, tab in enumerate(tabs.tables):
                    print(f"Tabla {j+1}:")
                    for row in tab.extract():
                        print(row)
        doc.close()
    except Exception as e:
        print(f"Error: {e}")

files = [
    r"c:\Users\drop1\Downloads\ACTA 9940 - DIGRAF - 20326859568 28042026.pdf",
    r"c:\Users\drop1\Downloads\Acta Detalle 009940 - DIGRAF 20326859568.pdf",
    r"c:\Users\drop1\Downloads\PP 163 - DIGRAF - 20326859568 - 28042026.pdf"
]

for f in files:
    analyze_pdf(f)
