Feature: Theme toggle
  As a visitor
  I want to switch between light and dark themes
  So that I can read comfortably and have my choice remembered

  Scenario: The site starts in the light theme by default
	Given I open the start page
	Then the page should use the light theme

  Scenario: Toggling switches to the dark theme
	Given I open the start page
	When I toggle the theme
	Then the page should use the dark theme

  Scenario: Toggling twice returns to the light theme
	Given I open the start page
	When I toggle the theme
	And I toggle the theme
	Then the page should use the light theme

  Scenario: The chosen theme is remembered after a reload
	Given I open the start page
	When I toggle the theme
	And I reload the page
	Then the page should use the dark theme
