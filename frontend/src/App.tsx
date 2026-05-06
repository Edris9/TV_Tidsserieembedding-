import './App.css'
import { Dashboard } from './components/Dashboard'

function App() {
  return (
    <main className="page">
      <header className="page__header">
        <img
          className="header-banner"
          src="/trafikverket-banner.jpg"
          alt="Trafikverket"
          width={262}
          height={144}
        />
      </header>
      <Dashboard />
    </main>
  )
}

export default App
