import { Routes, Route } from "react-router-dom";
import { Layout } from "./components/Layout";
import { HomePage } from "./pages/HomePage";
import { AssessmentPage } from "./pages/AssessmentPage";
import { ResultsPage } from "./pages/ResultsPage";
import { AuthPage } from "./pages/AuthPage";
import { ProfilePage, ProfileSkeleton } from "./pages/ProfilePage";
import { HistoricalResultPage } from "./pages/HistoricalResultPage";
import { RequireAuth } from "./components/RequireAuth";
import { AssessmentProvider } from "./context/AssessmentContext";
import { AuthProvider } from "./context/AuthContext";

function App() {
  return (
    <AuthProvider>
      <AssessmentProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/assessment" element={<AssessmentPage />} />
            <Route path="/results" element={<ResultsPage />} />
            <Route path="/login" element={<AuthPage mode="login" />} />
            <Route path="/signup" element={<AuthPage mode="signup" />} />
            <Route
              path="/profile"
              element={
                <RequireAuth fallback={<ProfileSkeleton />}>
                  <ProfilePage />
                </RequireAuth>
              }
            />
            <Route
              path="/history/:id"
              element={
                <RequireAuth fallback={<ProfileSkeleton />}>
                  <HistoricalResultPage />
                </RequireAuth>
              }
            />
          </Route>
        </Routes>
      </AssessmentProvider>
    </AuthProvider>
  );
}

export default App;
