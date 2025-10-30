using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticUI.Api.Services;

/// <summary>
/// Semantic Kernel plugin for generating FHIR-specific UI components
/// </summary>
public class FhirUIGenerationPlugin
{
    [KernelFunction, Description("Generates a React form for FHIR Patient resource editing")]
    public string GeneratePatientForm(
        [Description("Patient data in JSON format")] string patientDataJson,
        [Description("Form mode: 'create' or 'edit'")] string mode = "edit")
    {
        return $@"
Create a production-ready React form component for FHIR Patient resource with:
- Mode: {mode}
- Current patient data: {patientDataJson}
- Fields: name (HumanName), birthDate, gender, telecom (ContactPoint array), address (Address array)
- FHIR R4 specification compliance
- Field validation according to FHIR data types
- Tailwind CSS styling
- Call submitToChat({{ action: 'savePatient', patient: fhirPatientResource }}) on submit
- Display validation errors inline

Return ONLY the complete JSX code.
";
    }

    [KernelFunction, Description("Generates a React table for displaying FHIR Patient search results")]
    public string GeneratePatientTable(
        [Description("Array of Patient resources in JSON")] string patientsJson,
        [Description("Searchable fields")] string searchFields = "name,birthDate,gender")
    {
        return $@"
Create a sortable, filterable React table for FHIR Patient resources with:
- Data: {patientsJson}
- Searchable fields: {searchFields}
- Columns: Name, Birth Date, Gender, Contact Info
- Click handler: submitToChat({{ action: 'selectPatient', patientId: row.id }})
- Pagination (10 items per page)
- Tailwind CSS styling
- FHIR HumanName formatting helper function

Return ONLY the complete JSX code.
";
    }

    [KernelFunction, Description("Generates a React chart for population health analytics")]
    public string GeneratePopulationChart(
        [Description("Chart type: 'bar', 'line', 'pie'")] string chartType,
        [Description("Aggregated data in JSON format")] string dataJson,
        [Description("Chart title")] string title)
    {
        return $@"
Create an interactive chart component using Recharts library with:
- Type: {chartType}
- Data: {dataJson}
- Title: {title}
- Responsive design
- Tooltips with detailed information
- Click handler: submitToChat({{ action: 'chartSegmentClicked', data: segment }})
- Tailwind CSS styling
- Color palette: healthcare-friendly blues and greens

Include recharts in dependencies: {{ ""recharts"": ""^2.10.0"" }}

Return ONLY the complete JSX code.
";
    }

    [KernelFunction, Description("Generates a React form for FHIR Observation resource entry")]
    public string GenerateObservationForm(
        [Description("Observation type/code")] string observationType,
        [Description("Patient ID this observation is for")] string patientId,
        [Description("Value type: 'Quantity', 'String', 'CodeableConcept'")] string valueType = "Quantity")
    {
        return $@"
Create a FHIR Observation entry form with:
- Observation type: {observationType}
- Patient ID: {patientId}
- Value type: {valueType}
- Fields: effectiveDateTime, value[x], interpretation, note
- FHIR R4 Observation structure
- Unit of measure selection for Quantity values
- Call submitToChat({{ action: 'saveObservation', observation: fhirObservationResource }})
- Validation for required FHIR elements
- Tailwind CSS styling

Return ONLY the complete JSX code.
";
    }

    [KernelFunction, Description("Generates a timeline view for patient's clinical history")]
    public string GeneratePatientTimeline(
        [Description("Array of FHIR resources (Observations, Conditions, Procedures) in JSON")] string resourcesJson,
        [Description("Patient name")] string patientName)
    {
        return $@"
Create an interactive timeline component showing patient clinical history with:
- Patient: {patientName}
- Resources: {resourcesJson}
- Timeline sorted by date (newest first)
- Resource type icons and color coding
- Expandable cards for each event with full resource details
- Filter by resource type
- Click handler: submitToChat({{ action: 'viewResource', resourceId: id, resourceType: type }})
- Tailwind CSS styling
- Vertical timeline with connecting lines

Return ONLY the complete JSX code.
";
    }

    [KernelFunction, Description("Generates a React dashboard for healthcare metrics")]
    public string GenerateDashboard(
        [Description("Dashboard metrics in JSON format")] string metricsJson,
        [Description("Dashboard title")] string title)
    {
        return $@"
Create a healthcare dashboard with:
- Title: {title}
- Metrics: {metricsJson}
- Layout: grid with cards for each metric
- Visual indicators: trend arrows, color-coded values
- Click handler for drill-down: submitToChat({{ action: 'viewMetricDetails', metric: metricName }})
- Responsive grid layout
- Recharts for mini sparklines
- Tailwind CSS styling

Include recharts dependency.

Return ONLY the complete JSX code.
";
    }
}
