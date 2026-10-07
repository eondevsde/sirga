using Microsoft.Extensions.Localization;
using MudBlazor;

namespace SIRGA.WebUI.Services;

/// <summary>
/// Traduce al ESPAÑOL los textos internos de MudBlazor: el paginador de las tablas, los botones de los
/// pasos, los selectores de fecha, los diálogos, la carga de archivos, etc.
///
/// Hacen falta porque MudBlazor trae esos textos **en inglés y no los toma de la cultura del sistema**: en
/// las pantallas se veían "PREVIOUS"/"NEXT" (los pasos de la DDJJ), "Rows per page:" (el paginador) y
/// "Ok"/"Cancel" (los diálogos), aunque el resto de la aplicación esté en español.
///
/// La CLAVE es el propio texto en inglés (así funciona MudBlazor por dentro), por eso el diccionario se lee
/// "texto en inglés" -> "texto en español". Si una clave no está traducida, se devuelve el original: no se
/// rompe nada, sólo queda en inglés.
///
/// **REGLA DEL PROYECTO: TODO EL FRONT VA EN ESPAÑOL.** Los textos propios de cada pantalla se escriben en
/// español directamente en el marcado; esto es sólo para lo que viene dentro de los componentes.
/// </summary>
public class MudLocalizerEspanol : MudLocalizer
{
  private static readonly Dictionary<string, string> Traducciones = new(StringComparer.Ordinal)
  {
    // ---- Tablas: paginador y estados ----
    ["Rows per page:"] = "Filas por página:",
    ["{0}-{1} of {2}"] = "{0}-{1} de {2}",
    ["First page"] = "Primera página",
    ["Last page"] = "Última página",
    ["Next page"] = "Página siguiente",
    ["Previous page"] = "Página anterior",
    ["Current page {0}"] = "Página actual {0}",
    ["Page {0}"] = "Página {0}",
    ["Loading..."] = "Cargando...",
    ["No matching records found"] = "Sin resultados",

    // ---- Pasos (MudStepper) ----
    ["Next"] = "Siguiente",
    ["Previous"] = "Anterior",
    ["Complete"] = "Finalizar",
    ["Reset"] = "Reiniciar",
    ["Skip"] = "Omitir",
    ["Start"] = "Inicio",
    ["End"] = "Fin",
    ["Contents"] = "Contenido",

    // ---- Diálogos y formularios ----
    ["Close"] = "Cerrar",
    ["Cancel"] = "Cancelar",
    ["Ok"] = "Aceptar",
    ["Save"] = "Guardar",
    ["Apply"] = "Aplicar",
    ["Clear"] = "Limpiar",
    ["Open"] = "Abrir",
    ["Browse files"] = "Elegir archivos",
    ["Drag files here or click to browse"] = "Arrastrá los archivos acá o hacé clic para elegirlos",
    ["File '{0}' exceeds the maximum allowed size of {1} bytes."] = "El archivo '{0}' supera el tamaño máximo permitido de {1} bytes.",
    ["Selected {0} files, which exceeds the maximum of {1}"] = "Se eligieron {0} archivos, que supera el máximo de {1}",
    ["Decrement"] = "Disminuir",
    ["Increment"] = "Aumentar",
    ["Toggle {0}"] = "Alternar {0}",

    // ---- Selectores de fecha ----
    ["Next month {0}"] = "Mes siguiente {0}",
    ["Next year {0}"] = "Año siguiente {0}",
    ["Previous month {0}"] = "Mes anterior {0}",
    ["Previous year {0}"] = "Año anterior {0}",
    ["Start date"] = "Fecha desde",
    ["End date"] = "Fecha hasta",

    // ---- Filtros y columnas (grillas) ----
    ["Add filter"] = "Agregar filtro",
    ["Clear filter"] = "Limpiar filtro",
    ["Open filters"] = "Abrir filtros",
    ["Remove filter"] = "Quitar filtro",
    ["Filter"] = "Filtro",
    ["Filter value"] = "Valor del filtro",
    ["Operator"] = "Operador",
    ["Column"] = "Columna",
    ["Columns"] = "Columnas",
    ["Column options"] = "Opciones de columna",
    ["Sort"] = "Ordenar",
    ["Unsort"] = "Quitar orden",
    ["Group"] = "Agrupar",
    ["Ungroup"] = "Desagrupar",
    ["Toggle group"] = "Mostrar u ocultar grupo",
    ["Expand group"] = "Expandir grupo",
    ["Collapse group"] = "Contraer grupo",
    ["Expand all groups"] = "Expandir todos los grupos",
    ["Collapse all groups"] = "Contraer todos los grupos",
    ["Hide"] = "Ocultar",
    ["Hide all"] = "Ocultar todo",
    ["Show all"] = "Mostrar todo",
    ["Move up"] = "Subir",
    ["Move down"] = "Bajar",
    ["Refresh data"] = "Actualizar datos",
    ["Select row"] = "Seleccionar fila",
    ["Select all rows"] = "Seleccionar todas las filas",
    ["Edit row"] = "Editar fila",
    ["Commit edit"] = "Confirmar edición",
    ["Cancel edit"] = "Cancelar edición",
    ["Value"] = "Valor",
    ["true"] = "sí",
    ["false"] = "no",

    // ---- Condiciones de filtro ----
    ["contains"] = "contiene",
    ["not contains"] = "no contiene",
    ["starts with"] = "empieza con",
    ["ends with"] = "termina con",
    ["equals"] = "es igual a",
    ["not equals"] = "es distinto de",
    ["is"] = "es",
    ["is not"] = "no es",
    ["is empty"] = "está vacío",
    ["is not empty"] = "no está vacío",
    ["is after"] = "es posterior a",
    ["is before"] = "es anterior a",
    ["is on or after"] = "es igual o posterior a",
    ["is on or before"] = "es igual o anterior a",

    // ---- Pestañas ----
    ["Add tab"] = "Agregar pestaña",
    ["Close tab"] = "Cerrar pestaña",
    ["Scroll tabs down"] = "Desplazar pestañas abajo",
    ["Scroll tabs left"] = "Desplazar pestañas a la izquierda",
    ["Scroll tabs right"] = "Desplazar pestañas a la derecha",
    ["Scroll tabs up"] = "Desplazar pestañas arriba",
    ["Collapse"] = "Contraer",
    ["Expand"] = "Expandir",

    // ---- Selector de color ----
    ["Alpha slider"] = "Control de transparencia",
    ["Grid"] = "Cuadrícula",
    ["Hide swatches"] = "Ocultar muestras",
    ["Show swatches"] = "Mostrar muestras",
    ["Hue slider"] = "Control de tono",
    ["Switch mode"] = "Cambiar modo",
    ["Palette"] = "Paleta",
    ["Spectrum"] = "Espectro",
    ["All"] = "Todos",
    ["Less"] = "Menos",
    ["More"] = "Más",
    ["Index {0}"] = "Índice {0}",
    ["{0} rating"] = "Puntaje {0}",

    // ---- Validación y conversión ----
    ["Not a valid number"] = "No es un número válido",
    ["Not a valid boolean"] = "No es un valor válido",
    ["Not a valid date time"] = "No es una fecha y hora válida",
    ["Not a valid time span"] = "No es un intervalo de tiempo válido",
    ["Not a valid {0}"] = "No es un valor válido de {0}",
    ["Conversion error: {0}"] = "Error de conversión: {0}",

    // ---- Aviso al salir con cambios sin guardar ----
    [".Leave site? Changes you made may not be saved."] = ".¿Salir del sitio? Puede que los cambios no se guarden.",
    ["Confirm Navigation"] = "Confirmar la salida"
  };

  public override LocalizedString this[string key]
  {
    get
    {
      if (Traducciones.TryGetValue(key, out var traduccion))
        return new LocalizedString(key, traduccion);

      // Sin traducción: se devuelve el original (queda en inglés, no se rompe).
      return new LocalizedString(key, key, resourceNotFound: true);
    }
  }
}
