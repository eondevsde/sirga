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
/// **OJO CON LA CLAVE**: MudBlazor NO pasa el texto en inglés, pasa el **NOMBRE del recurso** (por ejemplo
/// `MudStepper_Next`). Se comprobó por reflexión: los miembros de `MudBlazor.Resources.LanguageResource`
/// son constantes cuyo VALOR es el nombre, no el texto traducible. Traducir por el texto en inglés
/// ("Next") no tiene ningún efecto: por eso las claves de acá son los nombres (`MudStepper_Next`).
/// Al final hay además un puñado de alias por texto, por si algún componente usa el texto directo.
///
/// Si una clave no está traducida, se devuelve el original: queda en inglés, pero no se rompe nada.
///
/// **REGLA DEL PROYECTO: TODO EL FRONT VA EN ESPAÑOL.** Los textos propios de cada pantalla se escriben en
/// español directamente en el marcado; esto es sólo para lo que viene dentro de los componentes.
/// </summary>
public class MudLocalizerEspanol : MudLocalizer
{
  /// <summary>
  /// Claves = NOMBRES de recurso de MudBlazor (`MudBlazor.Resources.LanguageResource`), que es lo que los
  /// componentes le pasan al localizador.
  /// </summary>
  private static readonly Dictionary<string, string> PorNombreDeRecurso = new(StringComparer.Ordinal)
  {
    // ---------- Pasos (MudStepper) ----------
    ["MudStepper_Next"] = "Siguiente",
    ["MudStepper_Previous"] = "Anterior",
    ["MudStepper_Skip"] = "Omitir",
    ["MudStepper_Reset"] = "Reiniciar",
    ["MudStepper_Complete"] = "Finalizar",

    // ---------- Paginador ----------
    ["MudTablePager_FirstPage"] = "Primera página",
    ["MudTablePager_LastPage"] = "Última página",
    ["MudTablePager_NextPage"] = "Página siguiente",
    ["MudTablePager_PreviousPage"] = "Página anterior",
    ["MudDataGridPager_FirstPage"] = "Primera página",
    ["MudDataGridPager_LastPage"] = "Última página",
    ["MudDataGridPager_NextPage"] = "Página siguiente",
    ["MudDataGridPager_PreviousPage"] = "Página anterior",
    ["MudDataGridPager_RowsPerPage"] = "Filas por página:",
    ["MudDataGridPager_AllItems"] = "Todas",
    ["MudDataGridPager_InfoFormat"] = "{0}-{1} de {2}",
    ["MudPagination_FirstPage"] = "Primera página",
    ["MudPagination_LastPage"] = "Última página",
    ["MudPagination_NextPage"] = "Página siguiente",
    ["MudPagination_PreviousPage"] = "Página anterior",
    ["MudPagination_CurrentPage"] = "Página actual {0}",
    ["MudPagination_PageIndex"] = "Página {0}",

    // ---------- Tabla ----------
    ["MudTable_Loading"] = "Cargando…",
    ["MudTable_ExpandGroup"] = "Expandir grupo",
    ["MudTable_CollapseGroup"] = "Contraer grupo",
    ["MudTable_EditRow"] = "Editar fila",
    ["MudTable_CancelEditRow"] = "Cancelar edición",
    ["MudTable_CommitRow"] = "Confirmar edición",

    // ---------- Grilla de datos (MudDataGrid) ----------
    ["MudDataGrid_Apply"] = "Aplicar",
    ["MudDataGrid_Clear"] = "Limpiar",
    ["MudDataGrid_AddFilter"] = "Agregar filtro",
    ["MudDataGrid_Cancel"] = "Cancelar",
    ["MudDataGrid_Loading"] = "Cargando…",
    ["MudDataGrid_Save"] = "Guardar",
    ["MudDataGrid_HideAll"] = "Ocultar todo",
    ["MudDataGrid_ShowAll"] = "Mostrar todo",
    ["MudDataGrid_Columns"] = "Columnas",
    ["MudDataGrid_ExpandAllGroups"] = "Expandir todos los grupos",
    ["MudDataGrid_CollapseAllGroups"] = "Contraer todos los grupos",
    ["MudDataGrid_RefreshData"] = "Actualizar datos",
    ["MudDataGrid_True"] = "sí",
    ["MudDataGrid_False"] = "no",
    ["MudDataGrid_Unsort"] = "Quitar orden",
    ["MudDataGrid_Filter"] = "Filtro",
    ["MudDataGrid_Hide"] = "Ocultar",
    ["MudDataGrid_Ungroup"] = "Desagrupar",
    ["MudDataGrid_CollapseGroup"] = "Contraer grupo",
    ["MudDataGrid_ExpandGroup"] = "Expandir grupo",
    ["MudDataGrid_Group"] = "Agrupar",
    ["MudDataGrid_FilterValue"] = "Valor del filtro",
    ["MudDataGrid_Contains"] = "contiene",
    ["MudDataGrid_NotContains"] = "no contiene",
    ["MudDataGrid_Equals"] = "es igual a",
    ["MudDataGrid_NotEquals"] = "es distinto de",
    ["MudDataGrid_StartsWith"] = "empieza con",
    ["MudDataGrid_EndsWith"] = "termina con",
    ["MudDataGrid_IsEmpty"] = "está vacío",
    ["MudDataGrid_IsNotEmpty"] = "no está vacío",
    ["MudDataGrid_Is"] = "es",
    ["MudDataGrid_IsNot"] = "no es",
    ["MudDataGrid_IsAfter"] = "es posterior a",
    ["MudDataGrid_IsOnOrAfter"] = "es igual o posterior a",
    ["MudDataGrid_IsBefore"] = "es anterior a",
    ["MudDataGrid_IsOnOrBefore"] = "es igual o anterior a",
    ["MudDataGrid_Column"] = "Columna",
    ["MudDataGrid_Operator"] = "Operador",
    ["MudDataGrid_Value"] = "Valor",
    ["MudDataGrid_MoveDown"] = "Bajar",
    ["MudDataGrid_MoveUp"] = "Subir",
    ["MudDataGrid_Sort"] = "Ordenar",
    ["MudDataGrid_ClearFilter"] = "Limpiar filtro",
    ["MudDataGrid_OpenFilters"] = "Abrir filtros",
    ["MudDataGrid_ToggleGroupExpansion"] = "Mostrar u ocultar grupo",
    ["MudDataGrid_RemoveFilter"] = "Quitar filtro",
    ["MudDataGrid_SelectRow"] = "Seleccionar fila",
    ["MudDataGrid_SelectAllRows"] = "Seleccionar todas las filas",
    ["MudDataGrid_ShowColumnOptions"] = "Opciones de columna",
    // Los signos (=, !=, >, >=, <, <=) no se traducen: se usan como símbolos.

    // ---------- Diálogos, chips, avisos ----------
    ["MudDialog_Close"] = "Cerrar",
    ["MudAlert_Close"] = "Cerrar",
    ["MudChip_Close"] = "Cerrar",
    ["MudSnackbar_Close"] = "Cerrar",
    ["MudColorPicker_Close"] = "Cerrar",

    // ---------- Entradas y selectores ----------
    ["MudInput_Clear"] = "Limpiar",
    ["MudInput_Decrement"] = "Disminuir",
    ["MudInput_Increment"] = "Aumentar",
    ["MudNavGroup_ToggleExpand"] = "Alternar {0}",
    ["MudTimePicker_Open"] = "Abrir",
    ["MudBaseDatePicker_Open"] = "Abrir",
    ["MudColorPicker_Open"] = "Abrir",
    ["MudBaseDatePicker_PrevYear"] = "Año anterior {0}",
    ["MudBaseDatePicker_NextYear"] = "Año siguiente {0}",
    ["MudBaseDatePicker_PrevMonth"] = "Mes anterior {0}",
    ["MudBaseDatePicker_NextMonth"] = "Mes siguiente {0}",
    ["MudRangeInput_Start"] = "Inicio",
    ["MudRangeInput_End"] = "Fin",
    ["MudDateRangePicker_Start"] = "Fecha desde",
    ["MudDateRangePicker_End"] = "Fecha hasta",

    // ---------- Selector de color, carrusel, calificación, mapa de calor ----------
    ["MudColorPicker_SpectrumView"] = "Espectro",
    ["MudColorPicker_GridView"] = "Cuadrícula",
    ["MudColorPicker_PaletteView"] = "Paleta",
    ["MudColorPicker_ShowSwatches"] = "Mostrar muestras",
    ["MudColorPicker_HideSwatches"] = "Ocultar muestras",
    ["MudColorPicker_HueSlider"] = "Control de tono",
    ["MudColorPicker_AlphaSlider"] = "Control de transparencia",
    ["MudColorPicker_ModeSwitch"] = "Cambiar modo",
    ["MudCarousel_Index"] = "Índice {0}",
    ["MudCarousel_Next"] = "Siguiente",
    ["MudCarousel_Previous"] = "Anterior",
    ["MudRatingItem_Label"] = "Puntaje {0}",
    ["HeatMap_Less"] = "Menos",
    ["HeatMap_More"] = "Más",

    // ---------- Pestañas y árbol ----------
    ["MudTabs_ScrollLeft"] = "Desplazar pestañas a la izquierda",
    ["MudTabs_ScrollRight"] = "Desplazar pestañas a la derecha",
    ["MudTabs_ScrollUp"] = "Desplazar pestañas arriba",
    ["MudTabs_ScrollDown"] = "Desplazar pestañas abajo",
    ["MudTabs_CloseTab"] = "Cerrar pestaña",
    ["MudTabs_AddTab"] = "Agregar pestaña",
    ["MudTreeView_ExpandItem"] = "Expandir",
    ["MudTreeView_CollapseItem"] = "Contraer",
    ["MudPageContentNavigation_NavMenu"] = "Contenido",

    // ---------- Carga de archivos ----------
    ["MudFileUpload_DragAndDrop"] = "Arrastrá los archivos acá o hacé clic para elegirlos",
    ["MudFileUpload_Button"] = "Elegir archivos",
    ["MudFileUpload_FileSizeError"] = "El archivo '{0}' supera el tamaño máximo permitido de {1} bytes.",
    ["MudFileUpload_MaximumFileCountExceeded"] = "Se eligieron {0} archivos, que supera el máximo de {1}",

    // ---------- Aviso al salir con cambios sin guardar ----------
    ["MudExitPrompt_Title"] = "Confirmar la salida",
    ["MudExitPrompt_Text"] = "¿Salir del sitio? Puede que los cambios no se guarden.",
    ["MudExitPrompt_Exit"] = "Aceptar",
    ["MudExitPrompt_Cancel"] = "Cancelar",

    // ---------- Validación y conversión ----------
    ["Converter_InvalidBoolean"] = "No es un valor válido",
    ["Converter_InvalidNumber"] = "No es un número válido",
    ["Converter_InvalidGUID"] = "No es un identificador válido",
    ["Converter_NotValueOf"] = "No es un valor de {0}",
    ["Converter_InvalidDateTime"] = "No es una fecha y hora válida",
    ["Converter_InvalidTimeSpan"] = "No es un intervalo de tiempo válido",
    ["Converter_InvalidType"] = "No es un valor válido de {0}",
    ["Converter_ConversionNotImplemented"] = "La conversión al tipo {0} no está implementada",
    ["Converter_ConversionError"] = "Error de conversión: {0}",
    ["Converter_ConversionFailed"] = "Falló la conversión de {0} a {1}: {2}",
    ["Converter_UnableToConvert"] = "No se puede convertir a {0} desde el tipo {1}"
  };

  /// <summary>
  /// Alias por TEXTO en inglés. MudBlazor usa los nombres de recurso, pero por si algún componente pasa el
  /// texto directo se cubren también los más visibles. No molesta: sólo son claves de más.
  /// </summary>
  private static readonly Dictionary<string, string> PorTexto = new(StringComparer.Ordinal)
  {
    ["Next"] = "Siguiente",
    ["Previous"] = "Anterior",
    ["Complete"] = "Finalizar",
    ["Reset"] = "Reiniciar",
    ["Skip"] = "Omitir",
    ["Close"] = "Cerrar",
    ["Cancel"] = "Cancelar",
    ["Ok"] = "Aceptar",
    ["Save"] = "Guardar",
    ["Apply"] = "Aplicar",
    ["Clear"] = "Limpiar",
    ["Loading..."] = "Cargando...",
    ["Rows per page:"] = "Filas por página:",
    ["{0}-{1} of {2}"] = "{0}-{1} de {2}",
    ["First page"] = "Primera página",
    ["Last page"] = "Última página",
    ["Next page"] = "Página siguiente",
    ["Previous page"] = "Página anterior",
    ["No matching records found"] = "Sin resultados",
    ["Browse files"] = "Elegir archivos",
    ["Open"] = "Abrir",
    ["Column"] = "Columna",
    ["Columns"] = "Columnas",
    ["Filter"] = "Filtro",
    ["Hide"] = "Ocultar",
    ["Hide all"] = "Ocultar todo",
    ["Show all"] = "Mostrar todo",
    ["true"] = "sí",
    ["false"] = "no"
  };

  public override LocalizedString this[string key]
  {
    get
    {
      if (PorNombreDeRecurso.TryGetValue(key, out var traduccion) ||
          PorTexto.TryGetValue(key, out traduccion))
      {
        return new LocalizedString(key, traduccion);
      }

      // Sin traducción: se devuelve el original (queda en inglés, no se rompe).
      return new LocalizedString(key, key, resourceNotFound: true);
    }
  }
}
