// Example FHIR Patient Edit Form
// This is what the LLM might generate when asked to edit a patient

import React, { useState } from 'react';
import { submitToChat } from './utils';

export default function PatientForm() {
  const [patient, setPatient] = useState({
    resourceType: 'Patient',
    id: 'patient-123',
    name: [
      {
        family: 'Doe',
        given: ['John', 'Michael']
      }
    ],
    gender: 'male',
    birthDate: '1980-05-15',
    telecom: [
      {
        system: 'phone',
        value: '555-0100',
        use: 'home'
      }
    ],
    address: [
      {
        use: 'home',
        line: ['123 Main St'],
        city: 'Springfield',
        state: 'IL',
        postalCode: '62701'
      }
    ]
  });

  const [errors, setErrors] = useState({});

  const updateName = (field, value) => {
    setPatient(prev => ({
      ...prev,
      name: [{
        ...prev.name[0],
        [field]: field === 'given' ? value.split(' ') : value
      }]
    }));
  };

  const updateTelecom = (value) => {
    setPatient(prev => ({
      ...prev,
      telecom: [{
        ...prev.telecom[0],
        value
      }]
    }));
  };

  const updateAddress = (field, value) => {
    setPatient(prev => ({
      ...prev,
      address: [{
        ...prev.address[0],
        [field]: field === 'line' ? [value] : value
      }]
    }));
  };

  const validate = () => {
    const newErrors = {};

    if (!patient.name[0].family) {
      newErrors.family = 'Family name is required';
    }

    if (!patient.name[0].given || patient.name[0].given.length === 0) {
      newErrors.given = 'Given name is required';
    }

    if (!patient.birthDate) {
      newErrors.birthDate = 'Birth date is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e) => {
    e.preventDefault();

    if (validate()) {
      submitToChat({
        action: 'savePatient',
        patient
      });
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
      <h2 className="text-2xl font-bold text-gray-800 mb-6">Edit Patient Information</h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Name Section */}
        <div className="bg-blue-50 p-4 rounded-lg">
          <h3 className="font-semibold text-gray-700 mb-3">Name</h3>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Family Name *
              </label>
              <input
                type="text"
                value={patient.name[0].family}
                onChange={(e) => updateName('family', e.target.value)}
                className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              {errors.family && (
                <p className="text-red-500 text-sm mt-1">{errors.family}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Given Name(s) *
              </label>
              <input
                type="text"
                value={patient.name[0].given.join(' ')}
                onChange={(e) => updateName('given', e.target.value)}
                className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              {errors.given && (
                <p className="text-red-500 text-sm mt-1">{errors.given}</p>
              )}
            </div>
          </div>
        </div>

        {/* Demographics */}
        <div className="bg-green-50 p-4 rounded-lg">
          <h3 className="font-semibold text-gray-700 mb-3">Demographics</h3>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Birth Date *
              </label>
              <input
                type="date"
                value={patient.birthDate}
                onChange={(e) => setPatient({...patient, birthDate: e.target.value})}
                className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              {errors.birthDate && (
                <p className="text-red-500 text-sm mt-1">{errors.birthDate}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Gender
              </label>
              <select
                value={patient.gender}
                onChange={(e) => setPatient({...patient, gender: e.target.value})}
                className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
                <option value="male">Male</option>
                <option value="female">Female</option>
                <option value="other">Other</option>
                <option value="unknown">Unknown</option>
              </select>
            </div>
          </div>
        </div>

        {/* Contact */}
        <div className="bg-purple-50 p-4 rounded-lg">
          <h3 className="font-semibold text-gray-700 mb-3">Contact</h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number
            </label>
            <input
              type="tel"
              value={patient.telecom[0].value}
              onChange={(e) => updateTelecom(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
        </div>

        {/* Address */}
        <div className="bg-yellow-50 p-4 rounded-lg">
          <h3 className="font-semibold text-gray-700 mb-3">Address</h3>

          <div className="space-y-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Street Address
              </label>
              <input
                type="text"
                value={patient.address[0].line[0]}
                onChange={(e) => updateAddress('line', e.target.value)}
                className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  City
                </label>
                <input
                  type="text"
                  value={patient.address[0].city}
                  onChange={(e) => updateAddress('city', e.target.value)}
                  className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  State
                </label>
                <input
                  type="text"
                  value={patient.address[0].state}
                  onChange={(e) => updateAddress('state', e.target.value)}
                  className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Postal Code
                </label>
                <input
                  type="text"
                  value={patient.address[0].postalCode}
                  onChange={(e) => updateAddress('postalCode', e.target.value)}
                  className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Submit Button */}
        <div className="flex space-x-3 pt-4">
          <button
            type="submit"
            className="flex-1 bg-blue-500 text-white py-3 rounded-lg hover:bg-blue-600 font-semibold transition-colors"
          >
            Save Patient
          </button>
          <button
            type="button"
            onClick={() => submitToChat({ action: 'cancel' })}
            className="px-6 bg-gray-200 text-gray-700 py-3 rounded-lg hover:bg-gray-300 font-semibold transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
