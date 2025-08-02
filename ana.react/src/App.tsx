import { useState } from 'react'
import { AuthProvider } from './contexts/AuthContext';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Authentication from './components/Authentication'; 
import ProtectedRoute from './components/ProtectedRoute'; 
import MainLayout from './components/MainLayout'; 
import NotFound from './components/NotFound'; 

const App = () => {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          {/* Authentication routes */}
          <Route path="/authentication/:action" element={<Authentication />} />
          
          {/* Protected routes */}
          <Route 
            path="/*" 
            element={
              <ProtectedRoute>
                <MainLayout>
                  <Routes>
                    <Route path="/" element={<div>Home Page</div>} />

                  </Routes>
                </MainLayout>
              </ProtectedRoute>
            } 
          />
          
          {/* 404 Not Found */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
};

export default App;
