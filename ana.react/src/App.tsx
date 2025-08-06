import { AuthProvider } from './contexts/AuthContext';
import { SelectedGroupProvider } from './contexts/SelectedGroupContext';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Authentication from './components/Authentication'; 
import ProtectedRoute from './components/ProtectedRoute'; 
import MainLayout from './components/MainLayout'; 
import NotFound from './components/NotFound'; 
import Home from './pages/Home';
import Members from './pages/Members';
import MyGroups from './pages/MyGroups';

const App = () => {
  return (
    <AuthProvider>
      <SelectedGroupProvider>
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
                      <Route path="/" element={<Home />} />
                      <Route path="/members" element={<Members />} />
                      <Route path="/mygroups" element={<MyGroups />} />
                    </Routes>
                  </MainLayout>
                </ProtectedRoute>
              } 
            />
            
            {/* 404 Not Found */}
            <Route path="*" element={<NotFound />} />
          </Routes>
        </Router>
      </SelectedGroupProvider>
    </AuthProvider>
  );
};

export default App;
