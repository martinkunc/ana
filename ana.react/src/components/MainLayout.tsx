import { ReactNode, useState, useEffect } from 'react';
import NavMenu from './NavMenu';
import LoginDisplay from './LoginDisplay';
import styles from './MainLayout.module.css';

interface MainLayoutProps {
  children: ReactNode;
}

const MainLayout = ({ children }: MainLayoutProps) => {

  return (
    <div className={styles.page}>
      
      <div className={styles.sidebar}>
        <NavMenu />
      </div>

      <main>
        <div className={`${styles.topRow} px-4`}>
          <LoginDisplay />
        </div>

        <article className={`${styles.content} px-4`}>
          {children}
        </article>
      </main>
    </div>
  );
};

export default MainLayout;