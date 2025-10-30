// Example Population Health Chart
// This is what the LLM might generate when asked to visualize population data

import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts';
import { submitToChat } from './utils';

export default function PopulationChart() {
  const data = [
    { ageGroup: '0-18', count: 150, male: 75, female: 75 },
    { ageGroup: '19-35', count: 280, male: 140, female: 140 },
    { ageGroup: '36-50', count: 320, male: 165, female: 155 },
    { ageGroup: '51-65', count: 210, male: 100, female: 110 },
    { ageGroup: '65+', count: 140, male: 60, female: 80 }
  ];

  const handleBarClick = (data) => {
    submitToChat({
      action: 'chartSegmentClicked',
      data: {
        ageGroup: data.ageGroup,
        count: data.count
      }
    });
  };

  return (
    <div className="max-w-4xl mx-auto p-6 bg-white rounded-lg shadow-lg">
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-800">
          Patient Age Distribution
        </h2>
        <p className="text-gray-600 mt-1">
          Total Patients: {data.reduce((sum, item) => sum + item.count, 0)}
        </p>
      </div>

      <ResponsiveContainer width="100%" height={400}>
        <BarChart
          data={data}
          margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="ageGroup" />
          <YAxis />
          <Tooltip
            contentStyle={{
              backgroundColor: 'white',
              border: '1px solid #ccc',
              borderRadius: '8px'
            }}
          />
          <Legend />
          <Bar
            dataKey="male"
            fill="#3B82F6"
            name="Male"
            onClick={handleBarClick}
            cursor="pointer"
          />
          <Bar
            dataKey="female"
            fill="#EC4899"
            name="Female"
            onClick={handleBarClick}
            cursor="pointer"
          />
        </BarChart>
      </ResponsiveContainer>

      <div className="mt-6 grid grid-cols-2 gap-4">
        <div className="bg-blue-50 p-4 rounded-lg">
          <h3 className="font-semibold text-blue-900">Male Patients</h3>
          <p className="text-3xl font-bold text-blue-700 mt-2">
            {data.reduce((sum, item) => sum + item.male, 0)}
          </p>
        </div>
        <div className="bg-pink-50 p-4 rounded-lg">
          <h3 className="font-semibold text-pink-900">Female Patients</h3>
          <p className="text-3xl font-bold text-pink-700 mt-2">
            {data.reduce((sum, item) => sum + item.female, 0)}
          </p>
        </div>
      </div>

      <div className="mt-4 text-center">
        <button
          onClick={() => submitToChat({ action: 'exportData', data })}
          className="bg-green-500 text-white px-6 py-2 rounded-lg hover:bg-green-600 transition-colors"
        >
          Export Data
        </button>
      </div>
    </div>
  );
}
