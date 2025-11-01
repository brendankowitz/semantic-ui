# SemanticUI Examples

This document provides example interactions and use cases for SemanticUI.

## Basic FHIR Operations

### Example 1: View Patient Information

**User:**
```
Show me information for patient with ID patient-123
```

**What happens:**
1. LLM recognizes this as a patient lookup request
2. Calls `GetPatient` function with ID "patient-123"
3. Receives FHIR Patient resource
4. Calls `GeneratePatientForm` function with patient data
5. Generates a React form pre-populated with patient information
6. Form is validated and rendered in the browser

**Generated Component:**
- FHIR-compliant patient form
- Displays: name, birth date, gender, contact info, address
- All fields are editable
- Submit button sends data back to chat

### Example 2: Search Patients

**User:**
```
Find all female patients
```

**What happens:**
1. LLM calls `SearchPatients` with parameters "gender:female"
2. Receives array of matching patients
3. Calls `GeneratePatientTable` to display results
4. Creates sortable, filterable table
5. Each row is clickable to view patient details

### Example 3: Population Analytics

**User:**
```
Create a bar chart showing patient distribution by age group
```

**What happens:**
1. LLM calls `GetPopulationStats` with metric type "age-distribution"
2. Receives aggregated data
3. Calls `GeneratePopulationChart` with data
4. Generates Recharts bar chart component
5. Includes interactive tooltips and click handlers

## Advanced Use Cases

### Example 4: Multi-Step Workflow

**User:**
```
I need to enter a new blood pressure reading for patient John Doe
```

**Conversation flow:**

**Assistant:** "I'll help you enter a blood pressure observation for John Doe. Let me find the patient first."

*[Calls GetPatient]*

**Assistant:** "Found patient John Doe (ID: patient-123). I'll create a form for entering the blood pressure observation."

*[Generates observation form with FHIR Observation structure]*

**User fills out form:**
- Systolic: 120 mmHg
- Diastolic: 80 mmHg
- Date: 2024-01-15
- Notes: "Patient resting, normal reading"

**User clicks Submit**

**Assistant:** "Blood pressure observation saved successfully. The reading of 120/80 mmHg is within normal range. Would you like to see the patient's blood pressure history?"

### Example 5: Dashboard Creation

**User:**
```
Create a dashboard showing key healthcare metrics for our patient population
```

**What happens:**
1. LLM calls multiple `GetPopulationStats` functions
2. Aggregates data: patient count, age distribution, condition prevalence, etc.
3. Calls `GenerateDashboard` with all metrics
4. Generates comprehensive dashboard with:
   - Total patient count cards
   - Age distribution chart
   - Gender pie chart
   - Top conditions list
   - Recent activity timeline

### Example 6: Patient Timeline

**User:**
```
Show me a timeline of all clinical events for patient Jane Smith
```

**What happens:**
1. LLM calls `GetPatient` to find Jane Smith
2. Calls `GetPatientObservations` for vital signs
3. Could call additional functions for conditions, procedures, medications
4. Calls `GeneratePatientTimeline` with all resources
5. Generates interactive timeline showing:
   - Chronological events
   - Color-coded by type (observation, condition, procedure)
   - Expandable cards with full resource details
   - Filter by resource type

## Component Examples

### Example 7: Custom Form Validation

**User:**
```
Create a form to enter a new patient with custom validation
```

**Generated component includes:**
```jsx
const validate = () => {
  const errors = {};

  // Name validation
  if (!patient.name[0].family) {
    errors.family = 'Family name is required';
  }

  // Birth date validation
  if (!patient.birthDate) {
    errors.birthDate = 'Birth date is required';
  } else {
    const birthDate = new Date(patient.birthDate);
    const today = new Date();
    if (birthDate > today) {
      errors.birthDate = 'Birth date cannot be in the future';
    }
  }

  // Phone validation
  if (patient.telecom[0].value &&
      !/^\d{3}-\d{4}$/.test(patient.telecom[0].value)) {
    errors.phone = 'Phone must be in format XXX-XXXX';
  }

  return errors;
};
```

### Example 8: Interactive Chart with Drill-Down

**User:**
```
Show me diabetes prevalence by age group, and let me click to see patient details
```

**Generated component:**
```jsx
const handleBarClick = (data) => {
  submitToChat({
    action: 'showPatients',
    condition: 'diabetes',
    ageGroup: data.ageGroup
  });
};

return (
  <BarChart data={data}>
    <Bar
      dataKey="count"
      fill="#3B82F6"
      onClick={handleBarClick}
      cursor="pointer"
    />
  </BarChart>
);
```

**When user clicks a bar:**
- Sends `action: 'showPatients'` with filter criteria
- LLM processes the interaction
- Generates a table showing patients in that age group with diabetes

## Integration Patterns

### Example 9: External FHIR Server Integration

Once you connect a real FHIR server, the flow becomes:

**User:**
```
Show me all patients with diabetes diagnosis
```

**Backend:**
```csharp
[KernelFunction]
public async Task<string> SearchPatients(string searchParams)
{
    // Real FHIR server call
    var client = new FhirClient("https://your-fhir-server/");
    var bundle = await client.SearchAsync<Patient>(
        new[] { $"condition=diabetes" }
    );

    return bundle.ToJson();
}
```

### Example 10: Save to Real FHIR Server

**User submits patient form:**

**Backend:**
```csharp
[KernelFunction]
public async Task<string> SavePatient(string patientJson)
{
    var client = new FhirClient("https://your-fhir-server/");
    var patient = JsonConvert.DeserializeObject<Patient>(patientJson);

    var result = await client.UpdateAsync(patient);

    return new {
        success = true,
        id = result.Id,
        versionId = result.Meta.VersionId
    }.ToJson();
}
```

## Error Handling

### Example 11: Validation Errors

If generated code contains security violations:

**User:**
```
Create a patient form
```

**LLM accidentally generates:**
```jsx
const handleSubmit = () => {
  eval(userInput); // Security violation!
};
```

**What happens:**
1. Backend AST validation catches `eval()`
2. Component is rejected
3. User sees: "The generated UI component contains security violations and cannot be rendered."
4. Error is logged for review
5. User can try asking again with different phrasing

## Performance Optimization

### Example 12: Caching Generated Components

For frequently requested components:

```csharp
public async Task<UIDefinition> GetOrGeneratePatientForm(string patientId)
{
    var cacheKey = $"patient-form-{patientId}";

    // Check cache
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        return JsonSerializer.Deserialize<UIDefinition>(cached);
    }

    // Generate new
    var component = await GeneratePatientForm(patientId);

    // Cache for 5 minutes
    await _cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(component),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }
    );

    return component;
}
```

## Tips for Best Results

### Be Specific

❌ **Vague:** "Show me patient data"
✅ **Specific:** "Show me patient John Doe's contact information and address in an editable form"

### Use Domain Language

✅ "Show vital signs observations for this patient"
✅ "Create a form for entering a FHIR Observation resource"
✅ "Display population statistics grouped by age and gender"

### Leverage Context

The LLM remembers conversation context:

```
User: Find patient John Doe
Assistant: [Displays patient info]

User: Now show me his vital signs
Assistant: [Knows "his" refers to John Doe]

User: Create a chart of his blood pressure over time
Assistant: [Generates chart for John Doe specifically]
```

### Request Iterations

```
User: Create a patient form
Assistant: [Generates basic form]

User: Add phone number validation
Assistant: [Updates form with validation]

User: Make the submit button green
Assistant: [Updates styling]
```

## Summary

SemanticUI enables:
- **Dynamic UI generation** based on conversational context
- **FHIR-compliant** components and data operations
- **Secure execution** with multiple validation layers
- **Interactive workflows** with back-and-forth refinement
- **Extensible architecture** for custom plugins and components

The key is treating the LLM as a dynamic application generator rather than just a chatbot.
