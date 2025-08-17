import { AppAuthProvider } from './contexts/AppAuthProvider';
import { SharedStateProvider } from './contexts/SharedStateContext';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Authentication from './components/Authentication'; 
import ProtectedRoute from './components/ProtectedRoute'; 
import MainLayout from './components/MainLayout'; 
import NotFound from './components/NotFound'; 
import Home from './pages/Home';
import Members from './pages/Members';
import MyGroups from './pages/MyGroups';
import Settings from './pages/Settings';

const App = () => {
  console.log("App component initialized. Environment mode:", import.meta.env.MODE);
  console.log("App component initialized. Environment dev:", import.meta.env.DEV);
  console.log("App component initialized. Environment prod:", import.meta.env.PROD);
  return (
  <AppAuthProvider>
      <SharedStateProvider>
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
                      <Route path="/settings" element={<Settings />} />
                    </Routes>
                  </MainLayout>
                </ProtectedRoute>
              } 
            />
            
            {/* 404 Not Found */}
            <Route path="*" element={<NotFound />} />
          </Routes>
        </Router>
      </SharedStateProvider>
    </AppAuthProvider>
  );
};

export default App;
