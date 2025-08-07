import React, { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useSharedState } from '../contexts/SharedStateContext';
import styles from './NavMenu.module.css';

interface NavMenuProps {
  className?: string;
  onNavToggle?: (isOpen: boolean) => void;
}

const NavMenu: React.FC<NavMenuProps> = ({ className = '' }) => {
  const [collapseNavMenu, setCollapseNavMenu] = useState(true);
  const { anaGroupName } = useSharedState();
  const location = useLocation();

  const navMenuCssClass = collapseNavMenu ? `collapse` : "";

  const toggleNavMenu = () => {
    setCollapseNavMenu(!collapseNavMenu);
    console.log(`NavMenu toggled: navMenuCssClass: ${navMenuCssClass}`);
  };


  // Navigation items configuration
  const navigationItems = [
    {
      href: '/',
      icon: `${styles.bi} ${styles.biHouseDoorFillNavMenu}`,
      label: 'Anniversaries',
      exact: true
    },
    {
      href: '/members',
      icon: `${styles.bi} ${styles.biHouseDoorFillNavMenu}`,
      label: 'Members',
      exact: true
    },
    {
      href: '/mygroups',
      icon: `${styles.bi} ${styles.biPlusSquareFillNavMenu}`,
      label: 'My other groups',
      exact: false
    },
    {
      href: '/settings',
      icon: `${styles.bi} ${styles.biListNestedNavMenu}`,
      label: 'Settings',
      exact: false
    }
  ];

  const isActiveLink = (item: typeof navigationItems[0]): boolean => {
    if (item.exact) {
      return location.pathname === item.href;
    }
    return location.pathname.startsWith(item.href);
  };

  return (
    <>
      {/* Top navigation bar */}
      <div className={`${styles.topRow} ps-3 navbar navbar-dark`}>
        <div className="container-fluid">
          <a className={`navbar-brand ${styles.navbarBrand}`} href="">
            {anaGroupName}
          </a>
          <button 
            title="Navigation menu" 
            className={`${styles.navbarToggler} navbar-toggler`}
            onClick={toggleNavMenu}
            type="button"
            aria-expanded={collapseNavMenu}
            aria-label="Toggle navigation"
          >
            <span className="navbar-toggler-icon"></span>
          </button>
        </div>
      </div>


      <div className={`${navMenuCssClass} ${styles.navScrollableClass}`}>
        <nav className="flex-column">
          {navigationItems.map((item) => (
            <div key={item.href} className={`${styles.navItem} px-3`}>
              <NavLink 
                to={item.href}
                className={() => 
                  `nav-link ${isActiveLink(item) ? 'active' : ''}`
                }
                end={item.exact}
              >
                <span 
                  className={item.icon} 
                  aria-hidden="true"
                ></span> 
                {item.label}
              </NavLink>
            </div>
          ))}
        </nav>
      </div>
    </>
  );
};

export default NavMenu;